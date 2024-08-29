using System.Diagnostics.Metrics;

namespace OtelTester.Otlp;

public static class DiagnosticsConfig
{
    public const string ServiceName = "OtelTester";
    public const string MyMeter = "MyMeter";

    public static void AddMeters(this IServiceCollection services)
    {
        services.AddSingleton<MyMeters>();
    }
}

public class MyMeters
{
    // probably don't expose these directly
    public readonly Counter<int> Counter;

    public readonly Histogram<int> Histogram;

    // https://learn.microsoft.com/en-us/dotnet/core/diagnostics/metrics-instrumentation

    public MyMeters(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(DiagnosticsConfig.MyMeter);
        Counter = meter.CreateCounter<int>("invoked", "#",
            "counts todos method invocations");
        Histogram = meter.CreateHistogram<int>("todo.histogram", "#",
            "histogram?");
    }
}
