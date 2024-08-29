using System.Text.Json;
using System.Text.Json.Serialization;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OtelTester;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0,
        AppJsonSerializerContext.Default);
});

builder.Services.AddOptions<Settings>()
    .Bind(builder.Configuration.GetSection(Settings.Key));

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resourceBuilder =>
        resourceBuilder.AddService("OtelTester"))
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation();
        metrics.AddOtlpExporter();
    })
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation();
        tracing.AddOtlpExporter();
    })
    ;
builder.Logging.ClearProviders();
var settings = builder.Configuration.GetValue<Settings>(Settings.Key) ?? throw new InvalidOperationException("Settings not found");
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

var app = builder.Build();

var sampleTodos = new Todo[]
{
    new(1, "Walk the dog"),
    new(2, "Do the dishes", DateOnly.FromDateTime(DateTime.Now)),
    new(3, "Do the laundry", DateOnly.FromDateTime(DateTime.Now.AddDays(1))),
    new(4, "Clean the bathroom"),
    new(5, "Clean the car", DateOnly.FromDateTime(DateTime.Now.AddDays(2))),
};

var todosApi = app.MapGroup("/todos");
todosApi.MapGet("/", (ILogger<Program> logger) =>
{
    logger.LogInformation("Todo length: {Length}", sampleTodos.Length);
    return sampleTodos.AsEnumerable();
});
todosApi.MapGet("/{id:int}",
    IResult (int id, ILogger<Program> logger) =>
    {
        logger.LogInformation("Called with id: {Id}", id);
        var todo = sampleTodos.FirstOrDefault(a => a.Id == id);
        if (todo is null)
        {
            return TypedResults.NotFound();
        }

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
