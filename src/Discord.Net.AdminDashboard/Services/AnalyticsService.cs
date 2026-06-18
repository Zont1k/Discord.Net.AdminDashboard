using System.Collections.Concurrent;
using System.Diagnostics;
using Discord.Net.AdminDashboard.Models;
using Discord.Net.Scheduler.Scheduling;
using Discord.Net.Scheduler.Scheduling.JobStore;
using Discord.WebSocket;

namespace Discord.Net.AdminDashboard.Services;

public sealed class AnalyticsService : IDisposable
{
    private readonly DiscordSocketClient _client;
    private readonly IJobStore _store;
    private readonly Process _process;
    private readonly DateTimeOffset _startedAt;
    private readonly Timer _timer;
    private readonly List<AnalyticsSnapshot> _snapshots = [];
    private readonly ConcurrentQueue<TimelineEntry> _timeline = new();
    private readonly object _lock = new();

    private const int MaxSnapshots = 120;
    private const int MaxTimeline = 100;

    private long _totalMessages;
    private long _totalCommands;
    private int _messagesThisTick;

    public AnalyticsService(DiscordSocketClient client, IJobStore store)
    {
        _client = client;
        _store = store;
        _process = Process.GetCurrentProcess();
        _startedAt = DateTimeOffset.UtcNow;
        _timer = new Timer(OnTick, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));

        _client.MessageReceived += OnMessageReceived;
        _client.UserJoined += OnUserJoined;
        _client.UserLeft += OnUserLeft;
        _client.GuildAvailable += OnGuildAvailable;
        _client.GuildUnavailable += OnGuildUnavailable;
        _client.LatencyUpdated += OnLatencyUpdated;
        _client.Disconnected += OnDisconnected;
        _client.Connected += OnConnected;
    }

    public IReadOnlyList<AnalyticsSnapshot> GetHistory()
    {
        lock (_lock) return [.. _snapshots];
    }

    public IReadOnlyList<TimelineEntry> GetTimeline(int count = 50)
    {
        return _timeline.Reverse().Take(count).Reverse().ToList();
    }

    public (double MessagesPerMinute, long TotalMessages, long TotalCommands) GetCounts()
    {
        var mpm = _snapshots.Count > 0
            ? Math.Round(_snapshots.Average(s => s.MessagesPerMinute), 1)
            : 0.0;
        return (mpm, Interlocked.Read(ref _totalMessages), Interlocked.Read(ref _totalCommands));
    }

    public (double AvgCpu, double AvgMemory, double PeakMemory) GetAverages()
    {
        lock (_lock)
        {
            if (_snapshots.Count == 0) return (0, 0, 0);
            var avgCpu = Math.Round(_snapshots.Average(s => s.CpuPercent), 1);
            var avgMem = Math.Round(_snapshots.Average(s => s.MemoryMb), 1);
            var peakMem = Math.Round(_snapshots.Max(s => s.MemoryMb), 1);
            return (avgCpu, avgMem, peakMem);
        }
    }

    public void TrackCommand(string commandName, string? userName = null, string? guildName = null)
    {
        Interlocked.Increment(ref _totalCommands);
        Interlocked.Increment(ref _commandsThisTick);

        var entry = new TimelineEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            Type = "command",
            Message = $"Command `{commandName}` executed",
            UserName = userName,
            GuildName = guildName,
        };
        PushTimeline(entry);
    }

    public void LogEvent(string type, string message, string? guildName = null)
    {
        var entry = new TimelineEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            Type = type,
            Message = message,
            GuildName = guildName,
        };
        PushTimeline(entry);
    }

    private void PushTimeline(TimelineEntry entry)
    {
        _timeline.Enqueue(entry);
        while (_timeline.Count > MaxTimeline)
            _timeline.TryDequeue(out _);
    }

    private async Task OnMessageReceived(SocketMessage message)
    {
        if (message.Author.IsBot) return;
        Interlocked.Increment(ref _totalMessages);
        Interlocked.Increment(ref _messagesThisTick);
        await Task.CompletedTask;
    }

    private async Task OnUserJoined(SocketGuildUser user)
    {
        var entry = new TimelineEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            Type = "user_join",
            Message = $"User **{user.Username}** joined",
            GuildName = user.Guild.Name,
        };
        PushTimeline(entry);
        await Task.CompletedTask;
    }

    private async Task OnUserLeft(SocketGuild guild, SocketUser user)
    {
        var entry = new TimelineEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            Type = "user_left",
            Message = $"User **{user.Username}** left",
            GuildName = guild.Name,
        };
        PushTimeline(entry);
        await Task.CompletedTask;
    }

    private async Task OnGuildAvailable(SocketGuild guild)
    {
        var entry = new TimelineEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            Type = "guild",
            Message = $"Guild **{guild.Name}** became available",
            GuildName = guild.Name,
        };
        PushTimeline(entry);
        await Task.CompletedTask;
    }

    private async Task OnGuildUnavailable(SocketGuild guild)
    {
        var entry = new TimelineEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            Type = "guild",
            Message = $"Guild **{guild.Name}** became unavailable",
            GuildName = guild.Name,
        };
        PushTimeline(entry);
        await Task.CompletedTask;
    }

    private async Task OnLatencyUpdated(int oldLatency, int newLatency)
    {
        await Task.CompletedTask;
    }

    private async Task OnDisconnected(Exception ex)
    {
        var entry = new TimelineEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            Type = "disconnect",
            Message = $"Bot disconnected: {ex.Message}",
        };
        PushTimeline(entry);
        await Task.CompletedTask;
    }

    private async Task OnConnected()
    {
        var entry = new TimelineEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            Type = "connect",
            Message = "Bot connected to Discord",
        };
        PushTimeline(entry);
        await Task.CompletedTask;
    }

    private int _commandsThisTick;

    private void OnTick(object? state)
    {
        _process.Refresh();

        var messagesThisTick = Interlocked.Exchange(ref _messagesThisTick, 0);
        var commandsThisTick = Interlocked.Exchange(ref _commandsThisTick, 0);
        var mpm = Math.Round(messagesThisTick * 6.0, 1);

        var uptime = DateTimeOffset.UtcNow - _startedAt;
        var cpu = _process.TotalProcessorTime.TotalSeconds;
        var cpuPercent = Math.Round(
            cpu / Environment.ProcessorCount / Math.Max(0.01, uptime.TotalSeconds) * 100, 1);

        var memory = Math.Round(_process.WorkingSet64 / 1024.0 / 1024.0, 1);

        var snapshot = new AnalyticsSnapshot
        {
            Timestamp = DateTimeOffset.UtcNow,
            GuildCount = _client.Guilds.Count,
            TotalUsers = _client.Guilds.Sum(g => g.MemberCount),
            OnlineUsers = _client.Guilds.Sum(g => g.Users.Count(u => u.Status == UserStatus.Online)),
            CpuPercent = cpuPercent,
            MemoryMb = memory,
            LatencyMs = _client.Latency,
            MessagesPerMinute = mpm,
            CommandsExecuted = commandsThisTick,
            ActiveJobs = _store.GetAllAsync().GetAwaiter().GetResult().Count,
        };

        lock (_lock)
        {
            _snapshots.Add(snapshot);
            while (_snapshots.Count > MaxSnapshots)
                _snapshots.RemoveAt(0);
        }
    }

    public void Dispose()
    {
        _timer.Dispose();
        _client.MessageReceived -= OnMessageReceived;
        _client.UserJoined -= OnUserJoined;
        _client.UserLeft -= OnUserLeft;
        _client.GuildAvailable -= OnGuildAvailable;
        _client.GuildUnavailable -= OnGuildUnavailable;
        _client.LatencyUpdated -= OnLatencyUpdated;
        _client.Disconnected -= OnDisconnected;
        _client.Connected -= OnConnected;
    }
}
