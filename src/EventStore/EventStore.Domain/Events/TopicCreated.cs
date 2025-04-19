using System.Text.Json;

namespace EventStore.Domain.Events;

public record EventSchema(string EventType, JsonDocument Schema);

public record TopicCreated
{
    public required string TopicName { get; init; }
    public required string Description { get; init; }
    public int Version { get; init; } = 1;
    public required DateTimeOffset CreatedAt { get; init; }
    public required IReadOnlyList<EventSchema> EventSchemas { get; init; }
}
