namespace Discord.Net.AdminDashboard.Models;

public sealed class AnalyticsSnapshot
{
    public DateTimeOffset Timestamp { get; init; }
    public int GuildCount { get; init; }
    public int TotalUsers { get; init; }
    public int OnlineUsers { get; init; }
    public double CpuPercent { get; init; }
    public double MemoryMb { get; init; }
    public long LatencyMs { get; init; }
    public int ActiveJobs { get; init; }
    public double MessagesPerMinute { get; init; }
    public int CommandsExecuted { get; init; }
}

public sealed class TimelineEntry
{
    public DateTimeOffset Timestamp { get; init; }
    public required string Type { get; init; }
    public required string Message { get; init; }
    public string? GuildName { get; init; }
    public string? UserName { get; init; }
}

public sealed class ApiError
{
    public required string Error { get; init; }
}

public sealed class ExtendedStats
{
    public required DashboardStats Current { get; init; }
    public required List<AnalyticsSnapshot> History { get; init; }
    public required List<TimelineEntry> Timeline { get; init; }
    public required double MessagesPerMinute { get; init; }
    public required double AvgCpu { get; init; }
    public required double AvgMemory { get; init; }
    public required double PeakMemory { get; init; }
    public required double TotalUptimeHours { get; init; }
}
