using System.Text.Json.Serialization;

namespace EventStore.Application.Health.Responses;

/// <summary>
/// Response model for health check results
/// </summary>
public class HealthCheckResponse(
    string status,
    DateTimeOffset timestamp,
    IReadOnlyCollection<ComponentHealthResponse> components)
{
    /// <summary>
    /// Gets the overall health status
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; } = status ?? throw new ArgumentNullException(nameof(status));

    /// <summary>
    /// Gets the timestamp when the health check was performed
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; } = timestamp;

    /// <summary>
    /// Gets the health status of individual components
    /// </summary>
    [JsonPropertyName("components")]
    public IReadOnlyCollection<ComponentHealthResponse> Components { get; } =
        components ?? throw new ArgumentNullException(nameof(components));
}