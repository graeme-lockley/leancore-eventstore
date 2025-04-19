using EventStore.Domain.Aggregates;
using EventStore.Domain.Events;
using Microsoft.Extensions.Logging;

namespace EventStore.Application.Events.Handlers;

public class TopicCreatedEventHandler : IDomainEventHandler<TopicCreated>
{
    private readonly TopicCatalog _topicCatalog;
    private readonly ILogger<TopicCreatedEventHandler> _logger;

    public TopicCreatedEventHandler(TopicCatalog topicCatalog, ILogger<TopicCreatedEventHandler> logger)
    {
        _topicCatalog = topicCatalog ?? throw new ArgumentNullException(nameof(topicCatalog));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(TopicCreated @event, CancellationToken cancellationToken = default)
    {
        try
        {
            var topic = Domain.Entities.Topic.Create(@event.TopicName, @event.Description, @event.EventSchemas);
            
            if (_topicCatalog.TopicExists(topic.Name))
            {
                _logger.LogWarning("Topic with name '{TopicName}' already exists", topic.Name);
                return;
            }

            await _topicCatalog.CreateTopicAsync(topic.Name, topic.Description, topic.EventSchemas, cancellationToken);
            
            _logger.LogInformation(
                "Successfully processed TopicCreated event for topic '{TopicName}'", 
                topic.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing TopicCreated event for topic '{TopicName}'",
                @event.TopicName);
            throw;
        }
    }
}