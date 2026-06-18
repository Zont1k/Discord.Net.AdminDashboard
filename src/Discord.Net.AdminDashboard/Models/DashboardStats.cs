namespace Discord.Net.AdminDashboard.Models;

public sealed class DashboardStats
{
    public int GuildCount { get; init; }
    public int TotalUsers { get; init; }
    public int OnlineUsers { get; init; }
    public int ActiveJobs { get; init; }
    public int PendingJobs { get; init; }
    public int FailedJobs { get; init; }
    public double UptimeHours { get; init; }
    public double CpuPercent { get; init; }
    public double MemoryMb { get; init; }
    public long LatencyMs { get; init; }
    public int MessagesSent { get; init; }
    public int CommandsExecuted { get; init; }
    public double CpuCores { get; init; }
    public double TotalMemoryMb { get; init; }
    public string? BotName { get; init; }
    public string? BotDiscriminator { get; init; }
    public string? BotAvatarUrl { get; init; }
    public DateTimeOffset BotCreatedAt { get; init; }
}
