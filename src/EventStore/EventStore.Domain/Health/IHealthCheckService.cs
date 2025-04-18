namespace EventStore.Domain.Health;

/// <summary>
/// Defines a service that manages and executes health checks
/// </summary>
public interface IHealthCheckService
{
    /// <summary>
    /// Executes all registered health checks
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns>A collection of health check results</returns>
    Task<IReadOnlyCollection<HealthCheckResult>> CheckAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a specific health check by component name
    /// </summary>
    /// <param name="componentName">The name of the component to check</param>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns>The result of the health check</returns>
    Task<HealthCheckResult> CheckComponentAsync(string componentName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a new health check
    /// </summary>
    /// <param name="healthCheck">The health check to register</param>
    void RegisterHealthCheck(IHealthCheck healthCheck);
} 