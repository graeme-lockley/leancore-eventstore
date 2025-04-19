using System.Text.Json;
using EventStore.Domain.Aggregates;
using EventStore.Domain.Events;
using FluentAssertions;
using Moq;
using Xunit;

namespace EventStore.Tests.Domain;

public class TopicCatalogTests
{
    private readonly Mock<IEventPublisher> _eventPublisher;
    private readonly TopicCatalog _catalog;

    public TopicCatalogTests()
    {
        _eventPublisher = new Mock<IEventPublisher>();
        _catalog = new TopicCatalog(_eventPublisher.Object);
    }

    [Fact]
    public async Task CreateTopicAsync_WithValidData_CreatesTopicAndPublishesEvent()
    {
        // Arrange
        const string name = "test-topic";
        const string description = "Test Topic";
        var eventSchemas = new[]
        {
            new EventSchema("TestEvent", JsonDocument.Parse("{}"))
        };

        // Act
        var topic = await _catalog.CreateTopicAsync(name, description, eventSchemas);

        // Assert
        topic.Should().NotBeNull();
        topic.Name.Should().Be(name);
        topic.Description.Should().Be(description);
        topic.EventSchemas.Should().BeEquivalentTo(eventSchemas);

        _eventPublisher.Verify(
            x => x.PublishAsync(
                "_configuration",
                It.Is<TopicCreated>(e =>
                    e.TopicName == name &&
                    e.Description == description &&
                    e.Version == 1),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateTopicAsync_WithDuplicateName_ThrowsInvalidOperationException()
    {
        // Arrange
        const string name = "test-topic";
        const string description = "Test Topic";
        var eventSchemas = new[]
        {
            new EventSchema("TestEvent", JsonDocument.Parse("{}"))
        };

        await _catalog.CreateTopicAsync(name, description, eventSchemas);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _catalog.CreateTopicAsync(name, description, eventSchemas));
    }

    [Fact]
    public async Task GetTopic_WithExistingName_ReturnsTopic()
    {
        // Arrange
        const string name = "test-topic";
        const string description = "Test Topic";
        var eventSchemas = new[]
        {
            new EventSchema("TestEvent", JsonDocument.Parse("{}"))
        };

        var createdTopic = await _catalog.CreateTopicAsync(name, description, eventSchemas);

        // Act
        var topic = _catalog.GetTopic(name);

        // Assert
        topic.Should().NotBeNull();
        topic.Should().Be(createdTopic);
    }

    [Fact]
    public void GetTopic_WithNonExistingName_ThrowsKeyNotFoundException()
    {
        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => _catalog.GetTopic("non-existing"));
    }
} 