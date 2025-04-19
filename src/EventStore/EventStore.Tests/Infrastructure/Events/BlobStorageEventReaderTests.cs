using System.Text.Json;
using Azure.Storage.Blobs;
using EventStore.Infrastructure.Azure;
using EventStore.Infrastructure.Events;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EventStore.Tests.Infrastructure.Events;

public class BlobStorageEventReaderTests
{
    private readonly IBlobServiceClient _wrappedClient;
    private readonly BlobStorageEventReader _reader;
    private readonly string _testTopicName;

    public BlobStorageEventReaderTests()
    {
        var blobServiceClient = new BlobServiceClient("UseDevelopmentStorage=true");
        _wrappedClient = new BlobServiceClientWrapper(blobServiceClient);
        var mockLogger = new Mock<ILogger<BlobStorageEventReader>>();
        _reader = new BlobStorageEventReader(_wrappedClient, mockLogger.Object);
        _testTopicName = $"test-topic-{Guid.NewGuid()}";
    }

    [Fact]
    public async Task GetEventsAsync_WhenTopicDoesNotExist_ReturnsEmptySequence()
    {
        // Use a topic name that definitely doesn't exist
        var nonExistentTopic = $"non-existent-{Guid.NewGuid()}";

        // Act
        var events = _reader.GetEventsAsync(nonExistentTopic);
        var eventsList = new List<object>();
        await foreach (var eventItem in events)
        {
            eventsList.Add(eventItem);
        }

        // Assert
        Assert.Empty(eventsList);
    }

    [Fact]
    public async Task GetEventsAsync_WhenTopicExists_ReturnsEvents()
    {
        try
        {
            // Arrange
            var containerClient = _wrappedClient.GetBlobContainerClient(_testTopicName);
            await containerClient.CreateIfNotExistsAsync();

            var testEvent = new { Id = Guid.NewGuid(), Message = "Test Event" };
            var publisher = new BlobStorageEventPublisher(_wrappedClient, Mock.Of<ILogger<BlobStorageEventPublisher>>());
            await publisher.PublishAsync(_testTopicName, testEvent);

            // Act
            var events = _reader.GetEventsAsync(_testTopicName);
            var eventsList = new List<object>();
            await foreach (var eventItem in events)
            {
                eventsList.Add(eventItem);
            }

            // Assert
            Assert.Single(eventsList);
            
            // Verify the event has the expected message property
            var eventJson = JsonSerializer.Serialize(eventsList[0]);
            using var doc = JsonDocument.Parse(eventJson);
            var root = doc.RootElement;
            var messageProperty = root.GetProperty("message");
            Assert.Equal("Test Event", messageProperty.GetString());
        }
        finally
        {
            // Cleanup
            await _wrappedClient.GetBlobContainerClient(_testTopicName).DeleteIfExistsAsync();
        }
    }

    [Fact]
    public async Task GetEventsAsync_WithType_WhenTopicExists_ReturnsTypedEvents()
    {
        try
        {
            // Arrange
            var containerClient = _wrappedClient.GetBlobContainerClient(_testTopicName);
            await containerClient.CreateIfNotExistsAsync();

            var testEvent = new TestEvent { Id = Guid.NewGuid(), Message = "Test Event" };
            var publisher = new BlobStorageEventPublisher(_wrappedClient, Mock.Of<ILogger<BlobStorageEventPublisher>>());
            await publisher.PublishAsync(_testTopicName, testEvent);

            // Act
            var events = _reader.GetEventsAsync<TestEvent>(_testTopicName);
            var eventsList = new List<TestEvent>();
            await foreach (var eventItem in events)
            {
                eventsList.Add(eventItem);
            }

            // Assert
            var resultEvent = Assert.Single(eventsList);
            Assert.NotNull(resultEvent);
            Assert.Equal(testEvent.Message, resultEvent.Message);
        }
        finally
        {
            // Cleanup
            await _wrappedClient.GetBlobContainerClient(_testTopicName).DeleteIfExistsAsync();
        }
    }

    private class TestEvent
    {
        public Guid Id { get; set; }
        public string Message { get; init; } = string.Empty;
    }
} 