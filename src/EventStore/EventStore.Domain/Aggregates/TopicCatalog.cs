using System.Collections.Concurrent;
using EventStore.Domain.Entities;
using EventStore.Domain.Events;

namespace EventStore.Domain.Aggregates;

public class TopicCatalog
{
    private readonly ConcurrentDictionary<string, Topic> _topics = new();

    public IReadOnlyDictionary<string, Topic> Topics => _topics;

    public Topic CreateTopic(string name, string description, IReadOnlyList<EventSchema> eventSchemas)
    {
        var topic = Topic.Create(name, description, eventSchemas);
        
        if (!_topics.TryAdd(topic.Name, topic))
        {
            throw new InvalidOperationException($"Topic with name '{name}' already exists");
        }

        // Raise TopicCreated domain event
        var topicCreated = new TopicCreated
        {
            TopicName = topic.Name,
            Description = topic.Description,
            Version = topic.Version,
            CreatedAt = topic.CreatedAt,
            EventSchemas = topic.EventSchemas
        };

        // TODO: Add domain event publishing mechanism
        
        return topic;
    }

    public Topic GetTopic(string name)
    {
        if (!_topics.TryGetValue(name, out var topic))
        {
            throw new KeyNotFoundException($"Topic '{name}' not found");
        }
        return topic;
    }

    public bool TopicExists(string name)
    {
        return _topics.ContainsKey(name);
    }

    public bool TryGetTopic(string name, out Topic? topic)
    {
        return _topics.TryGetValue(name, out topic);
    }
} 