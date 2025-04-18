using System.Text.Json.Serialization;

namespace EventStore.Application.Health.Responses;

/// <summary>
/// Response model for health check results
/// </summary>
public class HealthCheckResponse
{
    /// <summary>
    /// Gets the overall health status
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; }

    /// <summary>
    /// Gets the timestamp when the health check was performed
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets the health status of individual components
    /// </summary>
    [JsonPropertyName("components")]
    public IReadOnlyCollection<ComponentHealthResponse> Components { get; }

    public HealthCheckResponse(
        string status,
        DateTimeOffset timestamp,
        IReadOnlyCollection<ComponentHealthResponse> components)
    {
        Status = status ?? throw new ArgumentNullException(nameof(status));
        Timestamp = timestamp;
        Components = components ?? throw new ArgumentNullException(nameof(components));
    }
} 