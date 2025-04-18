using EventStore.Domain.Health;
using EventStore.Infrastructure.Health;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace EventStore.Tests.Infrastructure.Health;

public class SystemHealthCheckTests
{
    private readonly Mock<ILogger<SystemHealthCheck>> _logger;
    private readonly SystemHealthCheck _healthCheck;
    private readonly SystemHealthCheckOptions _options;

    public SystemHealthCheckTests()
    {
        _logger = new Mock<ILogger<SystemHealthCheck>>();
        _options = new SystemHealthCheckOptions
        {
            MemoryThresholds = new MemoryThresholds
            {
                DegradedBytes = 512L * 1024L * 1024L,    // 512 MB
                UnhealthyBytes = 1024L * 1024L * 1024L   // 1 GB
            },
            ThreadPoolThresholds = new ThreadPoolThresholds
            {
                DegradedUtilization = 0.7,   // 70%
                UnhealthyUtilization = 0.85  // 85%
            },
            IncludeDetailedInfo = true
        };

        var optionsMock = new Mock<IOptions<SystemHealthCheckOptions>>();
        optionsMock.Setup(x => x.Value).Returns(_options);

        _healthCheck = new SystemHealthCheck(_logger.Object, optionsMock.Object);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenSystemMetricsAreNormal_ReturnsHealthy()
    {
        // Act
        var result = await _healthCheck.CheckHealthAsync();

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().NotBeNull();
        
        // Verify metrics are present
        result.Data.Should().ContainKey("memoryUsagePercentage");
        result.Data.Should().ContainKey("threadPoolUsagePercentage");

        // Verify memory usage is non-negative
        ((double)result.Data["memoryUsagePercentage"]).Should().BeGreaterOrEqualTo(0);

        // Verify thread pool usage is between 0 and 100
        ((double)result.Data["threadPoolUsagePercentage"]).Should().BeGreaterOrEqualTo(0);
        ((double)result.Data["threadPoolUsagePercentage"]).Should().BeLessThanOrEqualTo(100);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenExceptionOccurs_ReturnsUnhealthy()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<SystemHealthCheck>>();
        mockLogger.Setup(x => x.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Throws(new Exception("Test exception"));

        var optionsMock = new Mock<IOptions<SystemHealthCheckOptions>>();
        optionsMock.Setup(x => x.Value).Returns(_options);

        var healthCheck = new SystemHealthCheck(mockLogger.Object, optionsMock.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().NotBeNull();
        result.Description.Should().Contain("Error checking system health");
    }

    [Fact]
    public async Task CheckHealthAsync_VerifyMetricRanges()
    {
        // Act
        var result = await _healthCheck.CheckHealthAsync();

        // Assert
        // Verify memory usage percentage is between 0 and 100
        ((double)result.Data["memoryUsagePercentage"]).Should().BeGreaterOrEqualTo(0);
        ((double)result.Data["memoryUsagePercentage"]).Should().BeLessThanOrEqualTo(100);

        // Verify thread pool usage percentage is between 0 and 100
        ((double)result.Data["threadPoolUsagePercentage"]).Should().BeGreaterOrEqualTo(0);
        ((double)result.Data["threadPoolUsagePercentage"]).Should().BeLessThanOrEqualTo(100);
    }
} 