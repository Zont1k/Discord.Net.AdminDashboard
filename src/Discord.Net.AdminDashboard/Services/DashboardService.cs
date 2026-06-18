using System.Diagnostics;
using Discord.Net.AdminDashboard.Models;
using Discord.Net.Scheduler.Scheduling;
using Discord.Net.Scheduler.Scheduling.JobStore;
using Discord.WebSocket;

namespace Discord.Net.AdminDashboard.Services;

public sealed class DashboardService
{
    private readonly DiscordSocketClient _client;
    private readonly IJobStore _store;
    private readonly JobScheduler _scheduler;
    private readonly DateTimeOffset _startedAt;
    private readonly Process _process;
    private readonly AnalyticsService _analytics;

    public DashboardService(
        DiscordSocketClient client,
        IJobStore store,
        JobScheduler scheduler,
        AnalyticsService analytics)
    {
        _client = client;
        _store = store;
        _scheduler = scheduler;
        _analytics = analytics;
        _startedAt = DateTimeOffset.UtcNow;
        _process = Process.GetCurrentProcess();
    }

    public async Task<DashboardStats> GetStatsAsync()
    {
        _process.Refresh();

        var allJobs = await _store.GetAllAsync();
        var pending = allJobs.Count(j => j.Status == JobStatus.Pending);
        var failed = allJobs.Count(j => j.Status == JobStatus.Failed);

        var uptime = DateTimeOffset.UtcNow - _startedAt;
        var cpu = _process.TotalProcessorTime.TotalSeconds;
        var cpuPercent = Math.Round(
            cpu / Environment.ProcessorCount / Math.Max(0.01, uptime.TotalSeconds) * 100, 1);

        var currentUser = _client.CurrentUser;
        var (mpm, totalMsg, totalCmd) = _analytics.GetCounts();

        return new DashboardStats
        {
            GuildCount = _client.Guilds.Count,
            TotalUsers = _client.Guilds.Sum(g => g.MemberCount),
            OnlineUsers = _client.Guilds.Sum(g => g.Users.Count(u => u.Status == UserStatus.Online)),
            ActiveJobs = allJobs.Count,
            PendingJobs = pending,
            FailedJobs = failed,
            UptimeHours = Math.Round(uptime.TotalHours, 1),
            CpuPercent = cpuPercent,
            CpuCores = Environment.ProcessorCount,
            MemoryMb = Math.Round(_process.WorkingSet64 / 1024.0 / 1024.0, 1),
            TotalMemoryMb = Math.Round(_process.WorkingSet64 / 1024.0 / 1024.0, 1),
            LatencyMs = _client.Latency,
            MessagesSent = (int)totalMsg,
            CommandsExecuted = (int)totalCmd,
            BotName = currentUser?.Username,
            BotDiscriminator = currentUser?.Discriminator,
            BotAvatarUrl = currentUser?.GetAvatarUrl(),
            BotCreatedAt = currentUser?.CreatedAt ?? DateTimeOffset.MinValue,
        };
    }

    public async Task<ExtendedStats> GetExtendedStatsAsync()
    {
        var current = await GetStatsAsync();
        var history = _analytics.GetHistory();
        var timeline = _analytics.GetTimeline();
        var (mpm, _, _) = _analytics.GetCounts();
        var (avgCpu, avgMem, peakMem) = _analytics.GetAverages();

        return new ExtendedStats
        {
            Current = current,
            History = [.. history],
            Timeline = [.. timeline],
            MessagesPerMinute = mpm,
            AvgCpu = avgCpu,
            AvgMemory = avgMem,
            PeakMemory = peakMem,
            TotalUptimeHours = current.UptimeHours,
        };
    }

    public List<GuildInfo> GetGuilds()
    {
        return _client.Guilds.Select(g => new GuildInfo
        {
            Id = g.Id,
            Name = g.Name,
            IconUrl = g.IconUrl,
            MemberCount = g.MemberCount,
            OnlineCount = g.Users.Count(u => u.Status == UserStatus.Online),
            ChannelCount = g.Channels.Count,
            RoleCount = g.Roles.Count,
            OwnerName = g.Owner?.Username ?? "Unknown",
            CreatedAt = g.CreatedAt,
        }).OrderByDescending(g => g.MemberCount).ToList();
    }

    public GuildInfo? GetGuild(ulong id)
    {
        var g = _client.GetGuild(id);
        if (g is null) return null;

        return new GuildInfo
        {
            Id = g.Id,
            Name = g.Name,
            IconUrl = g.IconUrl,
            MemberCount = g.MemberCount,
            OnlineCount = g.Users.Count(u => u.Status == UserStatus.Online),
            ChannelCount = g.Channels.Count,
            RoleCount = g.Roles.Count,
            OwnerName = g.Owner?.Username ?? "Unknown",
            CreatedAt = g.CreatedAt,
        };
    }

    public async Task<IReadOnlyList<IScheduledJob>> GetJobsAsync()
        => await _store.GetAllAsync();

    public async Task CancelJobAsync(string jobId)
        => await _scheduler.CancelAsync(jobId);

    public async Task RescheduleJobAsync(string jobId, DateTimeOffset newTime)
        => await _scheduler.RescheduleAsync(jobId, newTime);

}
