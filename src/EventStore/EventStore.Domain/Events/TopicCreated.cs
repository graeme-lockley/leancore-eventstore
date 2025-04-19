using System.Text.Json;
using System.Text.Json.Serialization;

namespace EventStore.Domain.Events;

public record EventSchema
{
    [JsonConstructor]
    public EventSchema(string eventType, JsonDocument schema)
    {
        EventType = eventType;
        Schema = schema;
    }

    [JsonPropertyName("eventType")]
    public string EventType { get; }

    [JsonPropertyName("schema")]
    public JsonDocument Schema { get; }
}

public record TopicCreated
{
    [JsonPropertyName("topicName")]
    public required string TopicName { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("version")]
    public int Version { get; init; } = 1;

    [JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("eventSchemas")]
    public required IReadOnlyList<EventSchema> EventSchemas { get; init; }
}
