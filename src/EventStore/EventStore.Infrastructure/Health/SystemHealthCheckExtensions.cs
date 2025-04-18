using Microsoft.Extensions.DependencyInjection;
using EventStore.Domain.Health;

namespace EventStore.Infrastructure.Health;

/// <summary>
/// Extension methods for registering the system health check
/// </summary>
public static class SystemHealthCheckExtensions
{
    /// <summary>
    /// Adds the system health check to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Optional action to configure the health check options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSystemHealthCheck(
        this IServiceCollection services,
        Action<SystemHealthCheckOptions>? configureOptions = null)
    {
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        services.AddOptions<SystemHealthCheckOptions>();
        services.AddScoped<SystemHealthCheck>();

        return services;
    }
} 