using Discord.Net.AdminDashboard.Hubs;
using Discord.Net.AdminDashboard.Models;
using Discord.Net.Scheduler.Scheduling;
using Discord.Net.Scheduler.Scheduling.JobStore;
using Discord.WebSocket;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Discord.Net.AdminDashboard.Services;

public sealed class DashboardHostedService : IHostedService, IAsyncDisposable
{
    private WebApplication? _app;
    private readonly DashboardOptions _options;
    private readonly DiscordSocketClient _client;
    private readonly IJobStore _store;
    private readonly JobScheduler _scheduler;
    private readonly ILogger<DashboardHostedService> _logger;

    public DashboardHostedService(
        DashboardOptions options,
        DiscordSocketClient client,
        IJobStore store,
        JobScheduler scheduler,
        ILogger<DashboardHostedService> logger)
    {
        _options = options;
        _client = client;
        _store = store;
        _scheduler = scheduler;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production,
        });

        builder.Services.Configure<KestrelServerOptions>(o =>
        {
            o.ListenLocalhost(_options.Port);
        });

        builder.Services.AddSignalR();
        builder.Services.AddLogging(o => o.AddConsole().SetMinimumLevel(LogLevel.Warning));

        builder.Services.AddSingleton(_client);
        builder.Services.AddSingleton(_store);
        builder.Services.AddSingleton(_scheduler);

        var analytics = new AnalyticsService(_client, _store);
        builder.Services.AddSingleton(analytics);
        builder.Services.AddSingleton(sp => new DashboardService(
            _client, _store, _scheduler, sp.GetRequiredService<AnalyticsService>()));

        var app = builder.Build();

        if (!string.IsNullOrEmpty(_options.AccessToken))
        {
            app.Use(async (ctx, next) =>
            {
                if (ctx.Request.Path.StartsWithSegments("/api"))
                {
                    var token = ctx.Request.Headers["Authorization"]
                        .FirstOrDefault()?.Replace("Bearer ", "");
                    if (token != _options.AccessToken)
                    {
                        ctx.Response.StatusCode = 401;
                        await ctx.Response.WriteAsync("Unauthorized");
                        return;
                    }
                }
                await next();
            });
        }

        var embeddedProvider = new EmbeddedFileProvider(
            typeof(DashboardHostedService).Assembly,
            "Discord.Net.AdminDashboard.wwwroot");

        app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = embeddedProvider });
        app.UseStaticFiles(new StaticFileOptions { FileProvider = embeddedProvider });

        app.MapHub<LiveLogHub>("/hubs/logs");

        app.MapGet("/api/stats", async (DashboardService ds) =>
            await ds.GetStatsAsync());

        app.MapGet("/api/extended", async (DashboardService ds) =>
            await ds.GetExtendedStatsAsync());

        app.MapGet("/api/guilds", (DashboardService ds) =>
            ds.GetGuilds());

        app.MapGet("/api/guilds/{id:long}", (ulong id, DashboardService ds) =>
        {
            var guild = ds.GetGuild(id);
            return guild is not null ? Results.Ok(guild) : Results.NotFound(new ApiError { Error = "Guild not found" });
        });

        app.MapGet("/api/jobs", async (DashboardService ds) =>
        {
            var jobs = await ds.GetJobsAsync();
            return jobs.Select(j => new JobInfo
            {
                Id = j.Id,
                JobType = j.GetType().Name,
                Status = j.Status.ToString(),
                NextExecution = j.NextExecution,
                CronExpression = j.CronExpression,
                CreatedAt = j.CreatedAt,
                LastError = j.LastError,
            }).ToList();
        });

        app.MapPost("/api/jobs/{id}/cancel", async (string id, DashboardService ds) =>
        {
            await ds.CancelJobAsync(id);
            return Results.Ok();
        });

        app.MapPost("/api/jobs/{id}/reschedule", async (string id, DateTimeOffset time, DashboardService ds) =>
        {
            await ds.RescheduleJobAsync(id, time);
            return Results.Ok();
        });

        app.MapGet("/api/analytics/history", (AnalyticsService analytics) =>
            analytics.GetHistory());

        app.MapGet("/api/analytics/timeline", (AnalyticsService analytics, int count = 50) =>
            analytics.GetTimeline(count));

        app.MapPost("/api/analytics/track-command", (TrackCommandRequest req, AnalyticsService analytics) =>
        {
            analytics.TrackCommand(req.Command, req.UserName, req.GuildName);
            return Results.Ok();
        });

        app.MapGet("/api/health", () => Results.Ok(new
        {
            status = "healthy",
            timestamp = DateTimeOffset.UtcNow,
            connected = _client.ConnectionState == Discord.ConnectionState.Connected,
        }));

        _app = app;
        _logger.LogInformation("Dashboard running at http://localhost:{Port}", _options.Port);
        await app.StartAsync(ct);
    }

    public async Task StopAsync(CancellationToken ct)
    {
        _logger.LogInformation("Dashboard stopping");
        if (_app is not null)
            await _app.StopAsync(ct);
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
            await _app.DisposeAsync();
    }
}

public sealed class DashboardOptions
{
    public int Port { get; set; } = 5000;
    public string? AccessToken { get; set; }
}

public sealed class TrackCommandRequest
{
    public required string Command { get; init; }
    public string? UserName { get; init; }
    public string? GuildName { get; init; }
}
