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
    private readonly SystemHealthCheckOptions _options;
    private readonly SystemHealthCheck _healthCheck;

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
                UnhealthyUtilization = 0.9    // 90%
            }
        };

        _healthCheck = new SystemHealthCheck(
            _logger.Object,
            Options.Create(_options));
    }

    [Fact]
    public async Task CheckHealthAsync_WhenSystemMetricsAreNormal_ReturnsHealthy()
    {
        // Act
        var result = await _healthCheck.CheckHealthAsync();

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Be("System is operating normally");
        
        // Verify metrics are present
        result.Data.Should().ContainKey("workingSetBytes");
        result.Data.Should().ContainKey("privateMemoryBytes");
        result.Data.Should().ContainKey("managedMemoryBytes");
        result.Data.Should().ContainKey("gcCollectionCount");
        result.Data.Should().ContainKey("threadPool");
        result.Data.Should().ContainKey("cpuTime");
        result.Data.Should().ContainKey("handles");
        result.Data.Should().ContainKey("threads");
        result.Data.Should().ContainKey("startTime");
        result.Data.Should().ContainKey("uptime");

        // Verify thread pool metrics
        var threadPool = (Dictionary<string, object>)result.Data["threadPool"];
        threadPool.Should().ContainKey("availableWorkerThreads");
        threadPool.Should().ContainKey("availableIoThreads");
        threadPool.Should().ContainKey("maxWorkerThreads");
        threadPool.Should().ContainKey("maxIoThreads");

        // Verify GC metrics
        var gcMetrics = (Dictionary<string, int>)result.Data["gcCollectionCount"];
        gcMetrics.Should().ContainKey("gen0");
        gcMetrics.Should().ContainKey("gen1");
        gcMetrics.Should().ContainKey("gen2");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenMemoryExceedsDegradedThreshold_ReturnsDegraded()
    {
        // Arrange
        var options = new SystemHealthCheckOptions
        {
            MemoryThresholds = new MemoryThresholds
            {
                DegradedBytes = 1L * 1024L * 1024L,      // 1 MB (Set very low to trigger degraded)
                UnhealthyBytes = 1024L * 1024L * 1024L   // 1 GB
            },
            ThreadPoolThresholds = new ThreadPoolThresholds
            {
                DegradedUtilization = 0.7,
                UnhealthyUtilization = 0.9
            }
        };

        var healthCheck = new SystemHealthCheck(_logger.Object, Options.Create(options));

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Be("System is experiencing high resource usage");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenExceptionOccurs_ReturnsUnhealthy()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<SystemHealthCheck>>();
        var mockOptions = new Mock<IOptions<SystemHealthCheckOptions>>();
        mockOptions.Setup(x => x.Value).Returns(new SystemHealthCheckOptions
        {
            MemoryThresholds = null!, // This will cause a NullReferenceException
            ThreadPoolThresholds = new ThreadPoolThresholds()
        });

        var healthCheck = new SystemHealthCheck(mockLogger.Object, mockOptions.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("Failed to collect system metrics");
        result.Data.Should().ContainKey("error");
        result.Data.Should().ContainKey("errorType");
        result.Data["errorType"].Should().Be("NullReferenceException");
    }

    [Fact]
    public async Task CheckHealthAsync_VerifyMetricRanges()
    {
        // Act
        var result = await _healthCheck.CheckHealthAsync();

        // Assert
        // Verify memory metrics are non-negative
        ((long)result.Data["workingSetBytes"]).Should().BeGreaterOrEqualTo(0);
        ((long)result.Data["privateMemoryBytes"]).Should().BeGreaterOrEqualTo(0);
        ((long)result.Data["managedMemoryBytes"]).Should().BeGreaterOrEqualTo(0);

        // Verify thread pool metrics are valid
        var threadPool = (Dictionary<string, object>)result.Data["threadPool"];
        ((int)threadPool["availableWorkerThreads"]).Should().BeGreaterOrEqualTo(0);
        ((int)threadPool["availableIoThreads"]).Should().BeGreaterOrEqualTo(0);
        ((int)threadPool["maxWorkerThreads"]).Should().BeGreaterThan(0);
        ((int)threadPool["maxIoThreads"]).Should().BeGreaterThan(0);

        // Verify GC collection counts are non-negative
        var gcMetrics = (Dictionary<string, int>)result.Data["gcCollectionCount"];
        gcMetrics["gen0"].Should().BeGreaterOrEqualTo(0);
        gcMetrics["gen1"].Should().BeGreaterOrEqualTo(0);
        gcMetrics["gen2"].Should().BeGreaterOrEqualTo(0);

        // Verify process metrics
        ((int)result.Data["handles"]).Should().BeGreaterOrEqualTo(0);
        ((int)result.Data["threads"]).Should().BeGreaterThan(0);
        ((DateTime)result.Data["startTime"]).Should().BeBefore(DateTime.UtcNow);
        ((TimeSpan)result.Data["uptime"]).Should().BeGreaterThan(TimeSpan.Zero);
    }
} 