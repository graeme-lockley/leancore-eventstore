using System.Text.Json.Serialization;

namespace EventStore.Application.Health.Responses;

/// <summary>
/// Represents the overall health status of the system.
/// </summary>
public class SystemHealthResponse
{
    /// <summary>
    /// Gets the overall status of the system.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; }

    /// <summary>
    /// Gets the timestamp when the health check was performed.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets the collection of component health check results.
    /// </summary>
    [JsonPropertyName("components")]
    public IReadOnlyCollection<ComponentHealthResponse> Components { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemHealthResponse"/> class.
    /// </summary>
    /// <param name="status">The overall system status.</param>
    /// <param name="components">The collection of component health check results.</param>
    /// <exception cref="ArgumentNullException">Thrown when status or components is null.</exception>
    public SystemHealthResponse(string status, IReadOnlyCollection<ComponentHealthResponse> components)
    {
        Status = status ?? throw new ArgumentNullException(nameof(status));
        Components = components ?? throw new ArgumentNullException(nameof(components));
        Timestamp = DateTimeOffset.UtcNow;
    }
} 