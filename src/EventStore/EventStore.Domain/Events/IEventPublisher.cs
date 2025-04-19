namespace EventStore.Domain.Events;

public interface IEventPublisher
{
    Task PublishAsync<TEvent>(string topicName, TEvent @event, CancellationToken cancellationToken = default) 
        where TEvent : class;
} 