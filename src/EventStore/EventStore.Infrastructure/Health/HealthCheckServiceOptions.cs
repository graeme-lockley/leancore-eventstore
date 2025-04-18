namespace EventStore.Infrastructure.Health;

/// <summary>
/// Configuration options for the health check service
/// </summary>
public class HealthCheckServiceOptions
{
    /// <summary>
    /// Gets or sets whether caching is enabled for health check results
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Gets or sets the duration for which health check results are cached
    /// </summary>
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets whether parallel execution of health checks is enabled
    /// </summary>
    public bool EnableParallelExecution { get; set; } = true;

    /// <summary>
    /// Gets or sets the timeout for individual health checks
    /// </summary>
    public TimeSpan HealthCheckTimeout { get; set; } = TimeSpan.FromSeconds(5);
} 