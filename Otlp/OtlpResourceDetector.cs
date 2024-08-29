using System.Runtime.InteropServices;
using OpenTelemetry.Resources;

namespace OtelTester.Otlp;

public class OtlpResourceDetector(VersionProvider versionProvider, IHostEnvironment environment)
    : IResourceDetector
{
    // https://opentelemetry.io/docs/specs/otel/semantic-conventions/
    public Resource Detect()
    {
        return new Resource(
        [
            new("service.version", versionProvider.Version),
            new ("host.name", Environment.MachineName),
            new ("dotnet.version", RuntimeInformation.FrameworkDescription),
            new ("environment.name", environment.EnvironmentName),
        ]);
    }
}
