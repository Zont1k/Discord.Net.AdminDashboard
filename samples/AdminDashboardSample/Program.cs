using Discord;
using Discord.Net.Scheduler.Scheduling.JobStore;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var discordToken = Environment.GetEnvironmentVariable("DISCORD_TOKEN")
    ?? throw new InvalidOperationException("Set DISCORD_TOKEN environment variable.");

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        var client = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.All,
            LogLevel = LogSeverity.Info,
        });

        services.AddSingleton(client);
        services.AddDiscordScheduler()
                .AddJobStore<InMemoryJobStore>();

        services.AddAdminDashboard(options =>
        {
            options.Port = 5000;
        });

        services.AddHostedService<BotHost>(sp =>
        {
            var c = sp.GetRequiredService<DiscordSocketClient>();
            var l = sp.GetRequiredService<ILogger<BotHost>>();
            return new BotHost(c, l, discordToken);
        });
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
    })
    .Build();

await host.RunAsync();

file sealed class BotHost : IHostedService
{
    private readonly DiscordSocketClient _client;
    private readonly ILogger<BotHost> _logger;
    private readonly string _token;

    public BotHost(DiscordSocketClient client, ILogger<BotHost> logger, string token)
    {
        _client = client;
        _logger = logger;
        _token = token;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        _client.Log += msg =>
        {
            _logger.LogInformation("[Discord] {Message}", msg.Message);
            return Task.CompletedTask;
        };

        await _client.LoginAsync(TokenType.Bot, _token);
        await _client.StartAsync();

        _logger.LogInformation("Bot started. Dashboard at http://localhost:5000");
    }

    public async Task StopAsync(CancellationToken ct)
    {
        await _client.StopAsync();
        _logger.LogInformation("Bot stopped");
    }
}
