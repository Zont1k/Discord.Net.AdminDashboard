namespace Discord.Net.AdminDashboard.Models;

public sealed class JobInfo
{
    public required string Id { get; init; }
    public required string JobType { get; init; }
    public required string Status { get; init; }
    public DateTimeOffset? NextExecution { get; init; }
    public string? CronExpression { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public string? LastError { get; init; }
    public int RetryCount { get; init; }
}
