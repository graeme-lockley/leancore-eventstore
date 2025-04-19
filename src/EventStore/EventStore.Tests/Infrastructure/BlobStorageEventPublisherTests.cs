using System.Text.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using EventStore.Infrastructure.Azure;
using EventStore.Infrastructure.Events;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EventStore.Tests.Infrastructure;

public class BlobStorageEventPublisherTests
{
    private readonly BlobStorageEventPublisher _publisher;
    private readonly string _testTopicName;
    private readonly IBlobServiceClient _wrappedClient;

    public BlobStorageEventPublisherTests()
    {
        var blobServiceClient = new BlobServiceClient("UseDevelopmentStorage=true");
        _wrappedClient = new BlobServiceClientWrapper(blobServiceClient);
        var mockLogger = new Mock<ILogger<BlobStorageEventPublisher>>();
        _publisher = new BlobStorageEventPublisher(_wrappedClient, mockLogger.Object);
        _testTopicName = $"test-topic-{Guid.NewGuid()}";
    }

    [Fact]
    public async Task PublishAsync_CreatesContainerAndUploadsEvent()
    {
        // Arrange
        var testEvent = new { Id = Guid.NewGuid(), Message = "Test Event" };

        try
        {
            // Act
            await _publisher.PublishAsync(_testTopicName, testEvent);

            // Assert
            var containerClient = _wrappedClient.GetBlobContainerClient(_testTopicName);
            var exists = await containerClient.ExistsAsync();
            Assert.True(exists);

            var blobs = containerClient.GetBlobsAsync();
            var blobsList = new List<BlobItem>();
            await foreach (var blobItem in blobs) blobsList.Add(blobItem);
            var singleBlob = Assert.Single(blobsList);

            var blobClient = containerClient.GetBlobClient(singleBlob.Name);
            var content = await blobClient.DownloadContentAsync();
            var eventJson = content.Value.Content.ToString();

            using (var doc = JsonDocument.Parse(eventJson))
            {
                var root = doc.RootElement;
                var messageProperty = root.GetProperty("message");
                Assert.Equal("Test Event", messageProperty.GetString());
            }
        }
        finally
        {
            // Cleanup
            await _wrappedClient.GetBlobContainerClient(_testTopicName).DeleteIfExistsAsync();
        }
    }

    [Fact]
    public async Task PublishAsync_WithMultipleEvents_CreatesContainerAndUploadsEvents()
    {
        // Arrange
        var testEvents = new[]
        {
            new { Id = Guid.NewGuid(), Message = "Test Event 1" },
            new { Id = Guid.NewGuid(), Message = "Test Event 2" }
        };

        try
        {
            // Act
            foreach (var testEvent in testEvents) await _publisher.PublishAsync(_testTopicName, testEvent);

            // Assert
            var containerClient = _wrappedClient.GetBlobContainerClient(_testTopicName);
            var exists = await containerClient.ExistsAsync();
            Assert.True(exists);

            var blobs = containerClient.GetBlobsAsync();
            var blobsList = new List<BlobItem>();
            await foreach (var blobItem in blobs) blobsList.Add(blobItem);
            Assert.Equal(testEvents.Length, blobsList.Count);
        }
        finally
        {
            // Cleanup
            await _wrappedClient.GetBlobContainerClient(_testTopicName).DeleteIfExistsAsync();
        }
    }

    [Fact]
    public async Task PublishAsync_WithTypedEvent_CreatesContainerAndUploadsEvent()
    {
        // Arrange
        var testEvent = new TestEvent { Id = Guid.NewGuid(), Message = "Test Event" };

        try
        {
            // Act
            await _publisher.PublishAsync(_testTopicName, testEvent);

            // Assert
            var containerClient = _wrappedClient.GetBlobContainerClient(_testTopicName);
            var exists = await containerClient.ExistsAsync();
            Assert.True(exists);

            var blobs = containerClient.GetBlobsAsync();
            var blobsList = new List<BlobItem>();
            await foreach (var blobItem in blobs) blobsList.Add(blobItem);
            var singleBlob = Assert.Single(blobsList);

            var blobClient = containerClient.GetBlobClient(singleBlob.Name);
            var content = await blobClient.DownloadContentAsync();
            var eventJson = content.Value.Content.ToString();

            var deserializedEvent = JsonSerializer.Deserialize<TestEvent>(eventJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(deserializedEvent);
            Assert.Equal(testEvent.Message, deserializedEvent.Message);
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
        public string Message { get; set; } = string.Empty;
    }
}