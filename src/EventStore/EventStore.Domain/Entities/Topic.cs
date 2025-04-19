using EventStore.Domain.Events;

namespace EventStore.Domain.Entities;

public class Topic
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public int Version { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public IReadOnlyList<EventSchema> EventSchemas { get; private set; }

    private Topic(string name, string description, IReadOnlyList<EventSchema> eventSchemas)
    {
        ValidateName(name);
        ValidateDescription(description);
        ValidateEventSchemas(eventSchemas);

        Name = name;
        Description = description;
        Version = 1;
        CreatedAt = DateTimeOffset.UtcNow;
        EventSchemas = eventSchemas;
    }

    public static Topic Create(string name, string description, IReadOnlyList<EventSchema> eventSchemas)
    {
        return new Topic(name, description, eventSchemas);
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Topic name cannot be empty", nameof(name));
            
        if (name.Length > 255)
            throw new ArgumentException("Topic name cannot exceed 255 characters", nameof(name));
            
        // Add additional validation rules for topic name format if needed
    }

    private static void ValidateDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Topic description cannot be empty", nameof(description));
    }

    private static void ValidateEventSchemas(IReadOnlyList<EventSchema> eventSchemas)
    {
        if (eventSchemas == null || !eventSchemas.Any())
            throw new ArgumentException("Topic must have at least one event schema", nameof(eventSchemas));
    }
} 