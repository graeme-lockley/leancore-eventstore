using System.Text.Json.Serialization;

namespace EventStore.Application.Health.Responses;

/// <summary>
/// Represents the health status response for an individual component.
/// </summary>
public class ComponentHealthResponse
{
    /// <summary>
    /// Gets the name of the component being checked.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; }

    /// <summary>
    /// Gets the health status of the component.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; }

    /// <summary>
    /// Gets additional details about the component's health status, if any.
    /// </summary>
    [JsonPropertyName("details")]
    public string? Details { get; }

    /// <summary>
    /// Gets any error messages associated with the health check, if any.
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; }

    /// <summary>
    /// Gets detailed metrics about the component's health, if any.
    /// For system health, this includes memory usage, thread pool status, and other performance metrics.
    /// For blob storage health, this includes response time and storage account details.
    /// </summary>
    [JsonPropertyName("metrics")]
    public IDictionary<string, object>? Metrics { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ComponentHealthResponse"/> class.
    /// </summary>
    /// <param name="name">The name of the component.</param>
    /// <param name="status">The health status of the component.</param>
    /// <param name="details">Optional additional details about the component's health.</param>
    /// <param name="error">Optional error message if the component is unhealthy.</param>
    /// <param name="metrics">Optional detailed metrics about the component's health.</param>
    /// <exception cref="ArgumentNullException">Thrown when name or status is null.</exception>
    public ComponentHealthResponse(
        string name, 
        string status, 
        string? details = null, 
        string? error = null,
        IDictionary<string, object>? metrics = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Status = status ?? throw new ArgumentNullException(nameof(status));
        Details = details;
        Error = error;
        Metrics = metrics;
    }
} 