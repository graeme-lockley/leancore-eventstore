using EventStore.Domain.Health;
using Microsoft.Extensions.DependencyInjection;

namespace EventStore.Infrastructure.Health;

/// <summary>
/// Extension methods for registering the blob storage health check
/// </summary>
public static class BlobStorageHealthCheckExtensions
{
    /// <summary>
    /// Adds the blob storage health check to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Optional action to configure the health check options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddBlobStorageHealthCheck(
        this IServiceCollection services,
        Action<BlobStorageHealthCheckOptions>? configureOptions = null)
    {
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        services.AddOptions<BlobStorageHealthCheckOptions>();
        services.AddScoped<IHealthCheck, BlobStorageHealthCheck>();

        return services;
    }
} 