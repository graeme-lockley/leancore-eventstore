using System.Text.Json.Serialization;

namespace EventStore.Application.Health.Responses;

/// <summary>
/// Represents the health status of an individual component
/// </summary>
public class ComponentHealthResponse
{
    /// <summary>
    /// Gets the name of the component
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; }

    /// <summary>
    /// Gets the status of the component (healthy, degraded, unhealthy)
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; }

    /// <summary>
    /// Gets the description or message about the component's health
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; }

    /// <summary>
    /// Gets the timestamp when the health check was performed
    /// </summary>
    [JsonPropertyName("lastChecked")]
    public DateTimeOffset LastChecked { get; }

    /// <summary>
    /// Gets additional details about the component's health
    /// </summary>
    [JsonPropertyName("details")]
    public IDictionary<string, object> Details { get; }

    public ComponentHealthResponse(
        string name,
        string status,
        string description,
        DateTimeOffset lastChecked,
        IDictionary<string, object> details)
    {
        Name = name;
        Status = status;
        Description = description;
        LastChecked = lastChecked;
        Details = details;
    }
} 