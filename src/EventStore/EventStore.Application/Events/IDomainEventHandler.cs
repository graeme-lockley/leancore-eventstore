namespace EventStore.Application.Events;

public interface IDomainEventHandler<in TEvent>
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
} 