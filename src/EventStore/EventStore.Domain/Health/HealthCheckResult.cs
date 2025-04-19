namespace EventStore.Domain.Health;

/// <summary>
/// Represents the result of a health check
/// </summary>
public class HealthCheckResult(
    string componentName,
    HealthStatus status,
    string description,
    IDictionary<string, object>? data = null)
{
    /// <summary>
    /// Gets the name of the component that was checked
    /// </summary>
    public string ComponentName { get; } = componentName ?? throw new ArgumentNullException(nameof(componentName));

    /// <summary>
    /// Gets the status of the health check
    /// </summary>
    public HealthStatus Status { get; } = status;

    /// <summary>
    /// Gets the description or message associated with the health check
    /// </summary>
    public string Description { get; } = description ?? throw new ArgumentNullException(nameof(description));

    /// <summary>
    /// Gets any additional data associated with the health check
    /// </summary>
    public IDictionary<string, object> Data { get; } = data ?? new Dictionary<string, object>();
}