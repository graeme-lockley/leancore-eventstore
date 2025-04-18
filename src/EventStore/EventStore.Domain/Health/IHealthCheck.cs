namespace EventStore.Domain.Health;

/// <summary>
/// Defines a health check that can be performed on a component
/// </summary>
public interface IHealthCheck
{
    /// <summary>
    /// Gets the name of the component being checked
    /// </summary>
    string ComponentName { get; }

    /// <summary>
    /// Performs the health check
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns>The result of the health check</returns>
    Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default);
} 