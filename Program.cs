using System.Text.Json;
using System.Text.Json.Serialization;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OtelTester;
using Serilog;
using Serilog.Debugging;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0,
        AppJsonSerializerContext.Default);
});

builder.Services.AddOptions<Settings>()
    .Bind(builder.Configuration.GetSection(Settings.Key));

builder.Services.AddMeters();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resourceBuilder =>
        resourceBuilder.AddService(DiagnosticsConfig.ServiceName))
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation();
        metrics.AddMeter(DiagnosticsConfig.MyMeter);
        metrics.AddOtlpExporter();
    })
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation();
        tracing.AddOtlpExporter();
    })
    ;
builder.Logging.ClearProviders();
var settings = builder.Configuration.GetSection(Settings.Key).Get<Settings>() ??
               throw new InvalidOperationException("Settings not found");
if (settings.LogProvider.HasFlag(LogProvider.OpenTelemetry))
{
    builder.Logging.AddOpenTelemetry(options =>
    {
        options.IncludeScopes = true;
        options.IncludeFormattedMessage = true;
        options.ParseStateValues = true;
        options.AddOtlpExporter();
    });
}

if (settings.LogProvider.HasFlag(LogProvider.Serilog))
{
    SelfLog.Enable(Console.Out);
    builder.Services.AddSerilog(configuration =>
    {
        configuration.MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.Seq("http://oteltester.seq:5341", apiKey: "x")
            .Enrich.FromLogContext();
    });
}

var app = builder.Build();
if (settings.LogProvider.HasFlag(LogProvider.Serilog))
{
    app.UseSerilogRequestLogging();
}

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
