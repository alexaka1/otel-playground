using System.Text.Json;
using System.Text.Json.Serialization;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OtelTester.Otlp;
using Serilog;
using Serilog.Debugging;

var builder = WebApplication.CreateSlimBuilder(args);
builder.Services.AddSingleton<VersionProvider>();
var version = new VersionProvider();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0,
        AppJsonSerializerContext.Default);
});

builder.Services.AddMeters();
builder.Services.AddSingleton<OtlpResourceDetector>();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resourceBuilder =>
        resourceBuilder.AddEnvironmentVariableDetector()
            .AddDetector(sp => sp.GetRequiredService<OtlpResourceDetector>()))
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation()
            .AddProcessInstrumentation()
            .AddRuntimeInstrumentation()
            .AddHttpClientInstrumentation();
        metrics.AddMeter(DiagnosticsConfig.MyMeter);
        metrics.AddOtlpExporter();
    })
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();
        tracing.AddOtlpExporter();
    })
    ;
builder.Logging.ClearProviders();

SelfLog.Enable(Console.Out);
// regular Serilog logging
builder.Services.AddSerilog((sp, configuration) =>
{
    var resource = sp.GetRequiredService<OtlpResourceDetector>().Detect();
    configuration
        .MinimumLevel.Information()
        .Enrich.FromLogContext()
        // regular logs
        .WriteTo.Console()
        .WriteTo.Seq("http://oteltester.seq:5341", apiKey: "x")
        // also send logs to OpenTelemetry, optional
        .WriteTo.OpenTelemetry(o =>
        {
            o.ResourceAttributes = new Dictionary<string, object>(resource.Attributes)
            {
                // https://opentelemetry.io/docs/specs/otel/semantic-conventions/
            };
        });
});


var app = builder.Build();
app.UseSerilogRequestLogging();

var sampleTodos = new Todo[]
{
    new(1, "Walk the dog"),
    new(2, "Do the dishes", DateOnly.FromDateTime(DateTime.Now)),
    new(3, "Do the laundry", DateOnly.FromDateTime(DateTime.Now.AddDays(1))),
    new(4, "Clean the bathroom"),
    new(5, "Clean the car", DateOnly.FromDateTime(DateTime.Now.AddDays(2))),
};

var todosApi = app.MapGroup("/todos");
todosApi.MapGet("/", (ILogger<Program> logger, MyMeters meter) =>
{
    logger.LogInformation("Todo length: {Length}", sampleTodos.Length);
    meter.Counter.Add(1,
        [new KeyValuePair<string, object?>("date", DateTime.Now)]);
    return sampleTodos.AsEnumerable();
});
todosApi.MapGet("/{id:int}",
    IResult (int id, ILogger<Program> logger, MyMeters meter) =>
    {
        logger.LogInformation("Called with id: {Id}", id);
        var todo = sampleTodos.FirstOrDefault(a => a.Id == id);
        if (todo is null)
        {
            return TypedResults.NotFound();
        }

        meter.Histogram.Record(Random.Shared.Next(),
            [new KeyValuePair<string, object?>("todo.id", todo.Id)]);

        logger.LogInformation("Returned: {@Todo}", todo);
        return TypedResults.Json(todo, AppJsonSerializerContext.Default.Todo);
    });

app.Run();

public record Todo(
    int Id,
    string? Title,
    DateOnly? DueBy = null,
    bool IsComplete = false);

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(Todo[]))]
[JsonSerializable(typeof(IEnumerable<Todo>))]
internal partial class AppJsonSerializerContext : JsonSerializerContext;
