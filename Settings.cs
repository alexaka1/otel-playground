namespace OtelTester;

public class Settings
{
    public LogProvider LogProvider { get; set; }
    public const string Key = "Settings";
}

[Flags]
public enum LogProvider
{
    None = 0,
    Serilog = 1,
    OpenTelemetry = 1 << 1,
}
