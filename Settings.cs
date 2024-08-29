namespace OtelTester;

public class Settings
{
    public const string Key = "Settings";

    public LogProvider LogProvider { get; set; }
}

[Flags]
public enum LogProvider
{
    None = 0,
    Serilog = 1,
    OpenTelemetry = 1 << 1,
}
