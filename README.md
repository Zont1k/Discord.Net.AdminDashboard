<p align="center">
  <img src="https://raw.githubusercontent.com/Zont1k/Discord.Net.AdminDashboard/main/assets/logo.png" alt="Discord.Net.AdminDashboard" width="200"/>
</p>

<h1 align="center">Discord.Net.AdminDashboard</h1>

<p align="center">
  <a href="https://www.nuget.org/packages/Discord.Net.AdminDashboard">
    <img src="https://img.shields.io/nuget/v/Discord.Net.AdminDashboard" alt="NuGet">
  </a>
  <a href="https://github.com/Zont1k/Discord.Net.AdminDashboard/actions">
    <img src="https://img.shields.io/github/actions/workflow/status/Zont1k/Discord.Net.AdminDashboard/ci.yml" alt="CI">
  </a>
  <a href="LICENSE">
    <img src="https://img.shields.io/github/license/Zont1k/Discord.Net.AdminDashboard" alt="License">
  </a>
</p>

<p align="center">
  One-line embedded admin dashboard for your Discord.NET bot.<br>
  Local web UI with guild stats, job management, and live logs.
</p>

---

## Features

- **Overview** — guild count, users, uptime, CPU/memory, latency
- **Guilds** — list all servers with member count, online users, channels
- **Jobs** — view and cancel scheduled jobs (requires `Discord.Net.Scheduler`)
- **Live Logs** — real-time streaming via SignalR, filterable log viewer
- **Zero config** — runs as `IHostedService` alongside your bot
- **No external dependencies** — pure ASP.NET Core Minimal API + SignalR

## Installation

```bash
dotnet add package Discord.Net.AdminDashboard
```

## Quick Start

```csharp
using Discord.Net.AdminDashboard.Extensions;
using Discord.Net.Scheduler.Extensions;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        services.AddSingleton(new DiscordSocketClient(...));
        services.AddDiscordScheduler()
                .AddJobStore<InMemoryJobStore>();

        services.AddAdminDashboard(options =>
        {
            options.Port = 5000;
        });
    })
    .Build();

await host.RunAsync();
// Dashboard available at http://localhost:5000
```

### With custom port

```csharp
services.AddAdminDashboard(port: 8080);
```

### With configuration delegate

```csharp
services.AddAdminDashboard(options =>
{
    options.Port = 5000;
    options.AccessToken = "my-secret"; // optional auth
});
```

## Pages

| Route | Page |
|-------|------|
| `/` | Overview dashboard with stats cards |
| `/guilds.html` | List of guilds with member counts |
| `/jobs.html` | Scheduled jobs table with cancel action |
| `/logs.html` | Live log stream via SignalR |

## API Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/stats` | Bot statistics |
| GET | `/api/guilds` | Guild list |
| GET | `/api/jobs` | Scheduled jobs |
| POST | `/api/jobs/{id}/cancel` | Cancel a job |

## Requirements

- .NET 8.0+
- Discord.NET 3.20.1+
- `Discord.Net.Scheduler` 1.0.0+ (for job management features)

## Dependencies

This package depends on:
- `Discord.Net.WebSocket` (3.20.1) — for accessing `DiscordSocketClient`
- `Discord.Net.Scheduler` (1.0.0) — for `IJobStore` and `JobScheduler`

## Screenshots

> (screenshots coming soon)

## License

[MIT](LICENSE)
