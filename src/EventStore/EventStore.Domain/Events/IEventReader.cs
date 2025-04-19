namespace EventStore.Domain.Events;

/// <summary>
/// Interface for reading events from a topic
/// </summary>
public interface IEventReader
{
    /// <summary>
    /// Gets all events from a topic
    /// </summary>
    /// <param name="topicName">The name of the topic to read events from</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>An async enumerable of events</returns>
    IAsyncEnumerable<object> GetEventsAsync(string topicName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all events of a specific type from a topic
    /// </summary>
    /// <typeparam name="TEvent">The type of events to retrieve</typeparam>
    /// <param name="topicName">The name of the topic to read events from</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>An async enumerable of typed events</returns>
    IAsyncEnumerable<TEvent> GetEventsAsync<TEvent>(string topicName, CancellationToken cancellationToken = default) 
        where TEvent : class;
} 