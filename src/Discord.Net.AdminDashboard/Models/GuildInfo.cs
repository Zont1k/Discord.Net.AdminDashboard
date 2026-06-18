using Discord.WebSocket;

namespace Discord.Net.AdminDashboard.Models;

public sealed class GuildInfo
{
    public required ulong Id { get; init; }
    public required string Name { get; init; }
    public string? IconUrl { get; init; }
    public required int MemberCount { get; init; }
    public required int OnlineCount { get; init; }
    public required int ChannelCount { get; init; }
    public required int RoleCount { get; init; }
    public required string OwnerName { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }

    public string Initials => Name.Length >= 2
        ? Name[..2].ToUpper()
        : Name.ToUpper();
}
