namespace Discord.Net.AdminDashboard.Models;

public sealed class LogEntry
{
    public required DateTimeOffset Timestamp { get; init; }
    public required string Level { get; init; }
    public required string Source { get; init; }
    public required string Message { get; init; }

    public string CssClass => Level.ToLower() switch
    {
        "error" or "critical" => "log-error",
        "warn" => "log-warn",
        "info" => "log-info",
        "debug" => "log-debug",
        _ => "log-info"
    };
}
