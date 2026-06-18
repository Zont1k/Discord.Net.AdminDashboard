# Changelog

## [1.0.0] - 2026-06-18

### Added
- Embedded admin dashboard for Discord.NET bots
- Minimal API + SignalR architecture (no Blazor dependency)
- Four dashboard pages: Overview, Guilds, Jobs, Live Logs
- Real-time log streaming via SignalR hub at `/hubs/logs`
- REST API endpoints: `/api/stats`, `/api/guilds`, `/api/jobs`, `/api/jobs/{id}/cancel`
- Discord-inspired hand-crafted dark theme CSS
- `AddAdminDashboard()` / `AddAdminDashboard(Action<DashboardOptions>)` DI extensions
- Targets `net8.0`, `net9.0`, `net10.0`
