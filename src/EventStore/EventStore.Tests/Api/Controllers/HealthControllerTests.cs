using EventStore.Api.Controllers;
using EventStore.Application.Health.Responses;
using EventStore.Domain.Health;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EventStore.Tests.Api.Controllers;

public class HealthControllerTests
{
    private readonly Mock<IHealthCheckService> _healthCheckService;
    private readonly HealthController _controller;

    public HealthControllerTests()
    {
        _healthCheckService = new Mock<IHealthCheckService>();
        var logger = new Mock<ILogger<HealthController>>();
        _controller = new HealthController(_healthCheckService.Object, logger.Object);
    }

    [Fact]
    public async Task GetHealth_WhenAllComponentsHealthy_ReturnsOkWithHealthyStatus()
    {
        // Arrange
        var healthyResults = new List<HealthCheckResult>
        {
            new("Component1", HealthStatus.Healthy, "Healthy", new Dictionary<string, object>()),
            new("Component2", HealthStatus.Healthy, "Healthy", new Dictionary<string, object>())
        };

        _healthCheckService
            .Setup(x => x.CheckAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthyResults);

        // Act
        var result = await _controller.GetHealth(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<HealthCheckResponse>().Subject;
        response.Status.Should().Be("Healthy");
        response.Components.Should().HaveCount(2);
        response.Components.Should().AllSatisfy(c => c.Status.Should().Be("Healthy"));
    }

    [Fact]
    public async Task GetHealth_WhenAnyComponentDegraded_ReturnsOkWithDegradedStatus()
    {
        // Arrange
        var mixedResults = new List<HealthCheckResult>
        {
            new("Component1", HealthStatus.Healthy, "Healthy", new Dictionary<string, object>()),
            new("Component2", HealthStatus.Degraded, "High memory usage", new Dictionary<string, object>())
        };

        _healthCheckService
            .Setup(x => x.CheckAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mixedResults);

        // Act
        var result = await _controller.GetHealth(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<HealthCheckResponse>().Subject;
        response.Status.Should().Be("Degraded");
        response.Components.Should().HaveCount(2);
        response.Components.Should().Contain(c => c.Status == "Degraded");
    }

    [Fact]
    public async Task GetHealth_WhenAnyComponentUnhealthy_ReturnsServiceUnavailableWithUnhealthyStatus()
    {
        // Arrange
        var unhealthyResults = new List<HealthCheckResult>
        {
            new("Component1", HealthStatus.Healthy, "Healthy", new Dictionary<string, object>()),
            new("Component2", HealthStatus.Unhealthy, "Service unavailable", new Dictionary<string, object>())
        };

        _healthCheckService
            .Setup(x => x.CheckAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(unhealthyResults);

        // Act
        var result = await _controller.GetHealth(CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(503);
        var response = statusResult.Value.Should().BeOfType<HealthCheckResponse>().Subject;
        response.Status.Should().Be("Unhealthy");
        response.Components.Should().HaveCount(2);
        response.Components.Should().Contain(c => c.Status == "Unhealthy");
    }

    [Fact]
    public async Task GetHealth_WhenServiceThrowsException_ReturnsServiceUnavailable()
    {
        // Arrange
        _healthCheckService
            .Setup(x => x.CheckAllAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _controller.GetHealth(CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(503);
        var response = statusResult.Value.Should().BeOfType<HealthCheckResponse>().Subject;
        response.Status.Should().Be("Unhealthy");
        response.Components.Should().HaveCount(1);
        response.Components.Should().ContainSingle(c => 
            c.Name == "System" && 
            c.Status == "Unhealthy" && 
            c.Error == "Error processing health check request");
    }
} 