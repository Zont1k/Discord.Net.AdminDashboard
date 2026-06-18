using Discord.Net.AdminDashboard.Models;
using Microsoft.AspNetCore.SignalR;

namespace Discord.Net.AdminDashboard.Hubs;

public sealed class LiveLogHub : Hub
{
    private static readonly int MaxEntries = 500;
    private static readonly List<LogEntry> _entries = [];
    private static readonly object _lock = new();

    public Task<List<LogEntry>> GetRecent()
    {
        lock (_lock)
            return Task.FromResult(_entries.ToList());
    }

    public static void Write(LogEntry entry)
    {
        lock (_lock)
        {
            _entries.Add(entry);
            if (_entries.Count > MaxEntries)
                _entries.RemoveRange(0, _entries.Count - MaxEntries);
        }
    }

    public static void Enqueue(LogEntry entry, IHubContext<LiveLogHub>? hub)
    {
        Write(entry);
        hub?.Clients.All.SendAsync("LogReceived", entry);
    }
}
