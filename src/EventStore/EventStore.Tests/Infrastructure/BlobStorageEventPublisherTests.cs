using System.Text.Json;
using Azure.Storage.Blobs;
using EventStore.Domain.Events;
using EventStore.Infrastructure.Events;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EventStore.Tests.Infrastructure;

public class BlobStorageEventPublisherTests : IAsyncLifetime
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly BlobStorageEventPublisher _publisher;
    private readonly string _testTopicName;

    public BlobStorageEventPublisherTests()
    {
        _blobServiceClient = new BlobServiceClient("UseDevelopmentStorage=true");
        var logger = new Mock<ILogger<BlobStorageEventPublisher>>();
        _publisher = new BlobStorageEventPublisher(_blobServiceClient, logger.Object);
        _testTopicName = $"test-topic-{Guid.NewGuid():N}".ToLowerInvariant();
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        // Cleanup: Delete the test container
        var containerClient = _blobServiceClient.GetBlobContainerClient(_testTopicName);
        await containerClient.DeleteIfExistsAsync();
    }

    [Fact]
    public async Task PublishAsync_WithValidEvent_StoresEventInBlobStorage()
    {
        // Arrange
        var schema = JsonDocument.Parse("{}");
        var testEvent = new TopicCreated
        {
            TopicName = "test",
            Description = "Test Topic",
            Version = 1,
            CreatedAt = DateTimeOffset.UtcNow,
            EventSchemas = [new EventSchema("TestEvent", schema)]
        };

        // Act
        await _publisher.PublishAsync(_testTopicName, testEvent);

        // Assert
        var containerClient = _blobServiceClient.GetBlobContainerClient(_testTopicName);
        var exists = await containerClient.ExistsAsync();
        exists.Value.Should().BeTrue();

        // Get the blob and verify its content
        var blobs = new List<string>();
        await foreach (var blob in containerClient.GetBlobsAsync())
        {
            blobs.Add(blob.Name);
        }
        blobs.Should().HaveCount(1);

        var blobClient = containerClient.GetBlobClient(blobs[0]);
        var content = await blobClient.DownloadContentAsync();
        var storedEvent = JsonSerializer.Deserialize<TopicCreated>(content.Value.Content);

        storedEvent.Should().NotBeNull();
        storedEvent!.TopicName.Should().Be(testEvent.TopicName);
        storedEvent.Description.Should().Be(testEvent.Description);
        storedEvent.Version.Should().Be(testEvent.Version);
        storedEvent.CreatedAt.Should().Be(testEvent.CreatedAt);
        storedEvent.EventSchemas.Should().NotBeNull();
        storedEvent.EventSchemas.Should().HaveCount(1);
        storedEvent.EventSchemas[0].EventType.Should().Be("TestEvent");
    }

    [Fact]
    public async Task PublishAsync_WithMultipleEvents_StoresAllEventsInBlobStorage()
    {
        // Arrange
        var schema1 = JsonDocument.Parse("{}");
        var schema2 = JsonDocument.Parse("{}");
        var testEvents = new[]
        {
            new TopicCreated
            {
                TopicName = "test1",
                Description = "Test Topic 1",
                Version = 1,
                CreatedAt = DateTimeOffset.UtcNow,
                EventSchemas = [new EventSchema("TestEvent1", schema1)]
            },
            new TopicCreated
            {
                TopicName = "test2",
                Description = "Test Topic 2",
                Version = 1,
                CreatedAt = DateTimeOffset.UtcNow,
                EventSchemas = [new EventSchema("TestEvent2", schema2)]
            }
        };

        // Act
        foreach (var testEvent in testEvents)
        {
            await _publisher.PublishAsync(_testTopicName, testEvent);
        }

        // Assert
        var containerClient = _blobServiceClient.GetBlobContainerClient(_testTopicName);
        var blobs = new List<string>();
        await foreach (var blob in containerClient.GetBlobsAsync())
        {
            blobs.Add(blob.Name);
        }
        blobs.Should().HaveCount(2);

        // Verify each stored event
        foreach (var blobName in blobs)
        {
            var blobClient = containerClient.GetBlobClient(blobName);
            var content = await blobClient.DownloadContentAsync();
            var storedEvent = JsonSerializer.Deserialize<TopicCreated>(content.Value.Content);

            storedEvent.Should().NotBeNull();
            storedEvent!.TopicName.Should().StartWith("test");
            storedEvent.Description.Should().StartWith("Test Topic");
            storedEvent.Version.Should().Be(1);
            storedEvent.CreatedAt.Should().NotBe(default);
            storedEvent.EventSchemas.Should().NotBeNull();
            storedEvent.EventSchemas.Should().HaveCount(1);
            storedEvent.EventSchemas[0].EventType.Should().StartWith("TestEvent");
        }
    }
} 