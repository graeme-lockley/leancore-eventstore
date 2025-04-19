using System.Collections.Concurrent;
using EventStore.Domain.Entities;
using EventStore.Domain.Events;

namespace EventStore.Domain.Aggregates;

public class TopicCatalog(IEventPublisher eventPublisher)
{
    private readonly ConcurrentDictionary<string, Topic> _topics = new();
    private readonly IEventPublisher _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));

    public IReadOnlyDictionary<string, Topic> Topics => _topics;

    public async Task<Topic> CreateTopicAsync(string name, string description, IReadOnlyList<EventSchema> eventSchemas, CancellationToken cancellationToken = default)
    {
        var topic = Topic.Create(name, description, eventSchemas);
        
        if (!_topics.TryAdd(topic.Name, topic))
        {
            throw new InvalidOperationException($"Topic with name '{name}' already exists");
        }

        var topicCreated = new TopicCreated
        {
            TopicName = topic.Name,
            Description = topic.Description,
            Version = topic.Version,
            CreatedAt = topic.CreatedAt,
            EventSchemas = topic.EventSchemas
        };

        await _eventPublisher.PublishAsync("_configuration", topicCreated, cancellationToken);
        
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