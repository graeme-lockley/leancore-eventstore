using System.Runtime.CompilerServices;
using System.Text.Json;
using EventStore.Domain.Events;
using EventStore.Infrastructure.Azure;
using Microsoft.Extensions.Logging;

namespace EventStore.Infrastructure.Events;

public class BlobStorageEventReader(
    IBlobServiceClient blobServiceClient,
    ILogger<BlobStorageEventReader> logger)
    : IEventReader
{
    private readonly IBlobServiceClient _blobServiceClient =
        blobServiceClient ?? throw new ArgumentNullException(nameof(blobServiceClient));

    private readonly ILogger<BlobStorageEventReader> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async IAsyncEnumerable<object> GetEventsAsync(
        string topicName,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(topicName.ToLowerInvariant());
        
        if (!await containerClient.ExistsAsync(cancellationToken))
        {
            _logger.LogWarning("Topic container '{TopicName}' does not exist", topicName);
            yield break;
        }

        await foreach (var blobItem in containerClient.GetBlobsAsync(cancellationToken: cancellationToken))
        {
            object? eventObj = null;
            
            try
            {
                var blobClient = containerClient.GetBlobClient(blobItem.Name);
                var content = await blobClient.DownloadContentAsync(cancellationToken);
                var eventJson = content.Value.Content.ToString();
                eventObj = JsonSerializer.Deserialize<object>(eventJson, _jsonSerializerOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error reading event from blob {BlobName} in topic {TopicName}",
                    blobItem.Name,
                    topicName);
            }

            if (eventObj != null)
            {
                yield return eventObj;
            }
        }
    }

    public async IAsyncEnumerable<TEvent> GetEventsAsync<TEvent>(
        string topicName,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TEvent : class
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(topicName.ToLowerInvariant());
        
        if (!await containerClient.ExistsAsync(cancellationToken))
        {
            _logger.LogWarning("Topic container '{TopicName}' does not exist", topicName);
            yield break;
        }

        await foreach (var blobItem in containerClient.GetBlobsAsync(cancellationToken: cancellationToken))
        {
            TEvent? eventObj = null;
            
            try
            {
                var blobClient = containerClient.GetBlobClient(blobItem.Name);
                var content = await blobClient.DownloadContentAsync(cancellationToken);
                var eventJson = content.Value.Content.ToString();
                eventObj = JsonSerializer.Deserialize<TEvent>(eventJson, _jsonSerializerOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error reading event of type {EventType} from blob {BlobName} in topic {TopicName}",
                    typeof(TEvent).Name,
                    blobItem.Name,
                    topicName);
            }

            if (eventObj != null)
            {
                yield return eventObj;
            }
        }
    }
} 