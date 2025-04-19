using System.Text.Json;
using Azure.Storage.Blobs;
using EventStore.Domain.Events;
using Microsoft.Extensions.Logging;

namespace EventStore.Infrastructure.Events;

public class BlobStorageEventPublisher(
    BlobServiceClient blobServiceClient,
    ILogger<BlobStorageEventPublisher> logger)
    : IEventPublisher
{
    private readonly BlobServiceClient _blobServiceClient =
        blobServiceClient ?? throw new ArgumentNullException(nameof(blobServiceClient));

    private readonly ILogger<BlobStorageEventPublisher> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task PublishAsync<TEvent>(string topicName, TEvent @event,
        CancellationToken cancellationToken = default)
        where TEvent : class
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(topicName.ToLowerInvariant());
            await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            var eventJson = JsonSerializer.Serialize(@event, _jsonSerializerOptions);
            var eventId = Guid.NewGuid().ToString();
            var timestamp = DateTimeOffset.UtcNow;
            var blobName = $"{timestamp:yyyy/MM/dd/HH/mm}/{eventId}.json";

            var blobClient = containerClient.GetBlobClient(blobName);
            await blobClient.UploadAsync(
                BinaryData.FromString(eventJson),
                overwrite: false,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Published event {EventType} to topic {TopicName} with ID {EventId}",
                typeof(TEvent).Name,
                topicName,
                eventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish event {EventType} to topic {TopicName}",
                typeof(TEvent).Name,
                topicName);
            throw;
        }
    }
}