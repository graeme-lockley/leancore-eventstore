using Azure;
using Azure.Storage.Blobs.Models;
using EventStore.Domain.Health;
using EventStore.Infrastructure.Health;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace EventStore.Tests.Infrastructure.Health;

public class BlobStorageHealthCheckTests
{
    private readonly Mock<IBlobServiceClient> _blobServiceClient;
    private readonly Mock<ILogger<BlobStorageHealthCheck>> _logger;
    private readonly BlobStorageHealthCheck _healthCheck;

    public BlobStorageHealthCheckTests()
    {
        _blobServiceClient = new Mock<IBlobServiceClient>();
        _logger = new Mock<ILogger<BlobStorageHealthCheck>>();
        var options = new BlobStorageHealthCheckOptions
        {
            TimeoutMs = 1000,
            IncludeDetailedInfo = true
        };

        var optionsMock = new Mock<IOptions<BlobStorageHealthCheckOptions>>();
        optionsMock.Setup(x => x.Value).Returns(options);

        _healthCheck = new BlobStorageHealthCheck(
            _logger.Object,
            _blobServiceClient.Object,
            optionsMock.Object);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenStorageIsAccessible_ReturnsHealthy()
    {
        // Arrange
        var mockResponse = new Mock<Response<BlobServiceProperties>>();
        var properties = new BlobServiceProperties
        {
            DefaultServiceVersion = "2020-06-12",
            StaticWebsite = new BlobStaticWebsite { Enabled = true }
        };
        mockResponse.Setup(x => x.Value).Returns(properties);

        _blobServiceClient
            .Setup(x => x.GetPropertiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse.Object);

        _blobServiceClient
            .Setup(x => x.AccountName)
            .Returns("testaccount");

        // Act
        var result = await _healthCheck.CheckHealthAsync();

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Be("Blob storage is accessible and responding");
        result.Data.Should().ContainKey("accountName");
        result.Data["accountName"].Should().Be("testaccount");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenStorageIsInaccessible_ReturnsUnhealthy()
    {
        // Arrange
        _blobServiceClient
            .Setup(x => x.GetPropertiesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException("Connection failed"));

        // Act
        var result = await _healthCheck.CheckHealthAsync();

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("Error accessing blob storage");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenOperationTimesOut_ReturnsDegraded()
    {
        // Arrange
        _blobServiceClient
            .Setup(x => x.GetPropertiesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act
        var result = await _healthCheck.CheckHealthAsync();

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Be("Unable to retrieve blob storage properties");
    }

    [Fact]
    public async Task CheckHealthAsync_WithDetailedInfoDisabled_ExcludesDetailedInfo()
    {
        // Arrange
        var options = new BlobStorageHealthCheckOptions
        {
            TimeoutMs = 1000,
            IncludeDetailedInfo = false
        };

        var optionsMock = new Mock<IOptions<BlobStorageHealthCheckOptions>>();
        optionsMock.Setup(x => x.Value).Returns(options);

        var mockResponse = new Mock<Response<BlobServiceProperties>>();
        var properties = new BlobServiceProperties
        {
            DefaultServiceVersion = "2020-06-12",
            StaticWebsite = new BlobStaticWebsite { Enabled = true }
        };
        mockResponse.Setup(x => x.Value).Returns(properties);

        _blobServiceClient
            .Setup(x => x.GetPropertiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse.Object);

        _blobServiceClient
            .Setup(x => x.AccountName)
            .Returns("testaccount");

        var healthCheck = new BlobStorageHealthCheck(
            _logger.Object,
            _blobServiceClient.Object,
            optionsMock.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data.Should().BeEmpty();
    }
} 