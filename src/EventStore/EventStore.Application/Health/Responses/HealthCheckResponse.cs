using System.Text.Json.Serialization;

namespace EventStore.Application.Health.Responses;

/// <summary>
/// Represents the overall health status of the system
/// </summary>
public class HealthCheckResponse
{
    /// <summary>
    /// Gets the overall status of the system (healthy, degraded, unhealthy)
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; }

    /// <summary>
    /// Gets the timestamp when the health check was performed
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets detailed health information for each component
    /// </summary>
    [JsonPropertyName("components")]
    public IReadOnlyCollection<ComponentHealthResponse> Components { get; }

    public HealthCheckResponse(
        string status,
        DateTimeOffset timestamp,
        IReadOnlyCollection<ComponentHealthResponse> components)
    {
        Status = status;
        Timestamp = timestamp;
        Components = components;
    }
} 