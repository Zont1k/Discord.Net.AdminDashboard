using Discord.Net.AdminDashboard.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAdminDashboard(
        this IServiceCollection services,
        int port = 5000,
        string? accessToken = null)
    {
        var options = new DashboardOptions
        {
            Port = port,
            AccessToken = accessToken,
        };

        services.AddSingleton(options);
        services.AddHostedService<DashboardHostedService>();

        return services;
    }

    public static IServiceCollection AddAdminDashboard(
        this IServiceCollection services,
        Action<DashboardOptions> configure)
    {
        var options = new DashboardOptions();
        configure(options);
        services.AddSingleton(options);
        services.AddHostedService<DashboardHostedService>();

        return services;
    }
}
