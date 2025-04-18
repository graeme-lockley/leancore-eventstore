namespace EventStore.Domain.Health;

/// <summary>
/// Represents the result of a health check
/// </summary>
public class HealthCheckResult
{
    /// <summary>
    /// Gets the name of the component that was checked
    /// </summary>
    public string ComponentName { get; }

    /// <summary>
    /// Gets the status of the health check
    /// </summary>
    public HealthStatus Status { get; }

    /// <summary>
    /// Gets the description or message associated with the health check
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the timestamp when the health check was performed
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets any additional data associated with the health check
    /// </summary>
    public IDictionary<string, object> Data { get; }

    public HealthCheckResult(
        string componentName,
        HealthStatus status,
        string description,
        IDictionary<string, object>? data = null)
    {
        ComponentName = componentName ?? throw new ArgumentNullException(nameof(componentName));
        Status = status;
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Timestamp = DateTimeOffset.UtcNow;
        Data = data ?? new Dictionary<string, object>();
    }
} 