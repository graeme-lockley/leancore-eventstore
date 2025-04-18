using EventStore.Domain.Health;
using Microsoft.Extensions.DependencyInjection;

namespace EventStore.Infrastructure.Health;

/// <summary>
/// Extension methods for registering the health check service
/// </summary>
public static class HealthCheckServiceExtensions
{
    /// <summary>
    /// Adds the health check service to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Optional action to configure the service options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddHealthCheckService(
        this IServiceCollection services,
        Action<HealthCheckServiceOptions>? configureOptions = null)
    {
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        services.AddOptions<HealthCheckServiceOptions>();
        services.AddSingleton<IHealthCheckService, HealthCheckService>();

        return services;
    }

    /// <summary>
    /// Adds a health check to the service collection and registers it with the health check service
    /// </summary>
    /// <typeparam name="THealthCheck">The type of the health check to add</typeparam>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddHealthCheck<THealthCheck>(this IServiceCollection services)
        where THealthCheck : class, IHealthCheck
    {
        services.AddScoped<THealthCheck>();
        services.AddScoped<IHealthCheck>(sp => sp.GetRequiredService<THealthCheck>());

        return services;
    }
} 