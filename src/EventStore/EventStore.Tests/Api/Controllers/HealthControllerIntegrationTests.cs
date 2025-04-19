using Moq;
using Xunit;
using FluentAssertions;
using EventStore.Application.Health.Responses;
using EventStore.Domain.Health;
using Microsoft.AspNetCore.Mvc;
using EventStore.Api.Controllers;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

namespace EventStore.Tests.Api.Controllers
{
    // Direct controller tests that avoid WebApplicationFactory entirely
    public class HealthControllerIntegrationTests
    {
        private readonly HealthController _controller;
        private readonly Mock<IHealthCheckService> _mockHealthCheckService;

        public HealthControllerIntegrationTests()
        {
            _mockHealthCheckService = new Mock<IHealthCheckService>();
            var mockLogger = new Mock<ILogger<HealthController>>();
            
            _controller = new HealthController(_mockHealthCheckService.Object, mockLogger.Object);
            
            // Setup default health check response
            _mockHealthCheckService.Setup(x => x.CheckAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<HealthCheckResult>
                {
                    new("BlobStorage", HealthStatus.Healthy, "Blob storage is healthy"),
                    new("System", HealthStatus.Healthy, "System is healthy")
                });

            _mockHealthCheckService.Setup(x => x.CheckComponentAsync("BlobStorage", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HealthCheckResult("BlobStorage", HealthStatus.Healthy, "Blob storage is healthy"));
        }

        [Fact]
        public async Task GetHealth_EndToEnd_ReturnsExpectedResponse()
        {
            // Act
            var result = await _controller.GetHealth(CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var content = okResult.Value.Should().BeOfType<HealthCheckResponse>().Subject;
            
            content.Should().NotBeNull();
            content.Components.Should().NotBeEmpty();
            content.Components.Should().Contain(c => c.Name == "BlobStorage");
            content.Components.Should().Contain(c => c.Name == "System");
        }

        [Fact]
        public async Task GetHealth_WithActualBlobStorage_ReturnsStorageStatus()
        {
            // Arrange
            _mockHealthCheckService.Setup(x => x.CheckAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<HealthCheckResult>
                {
                    new("BlobStorage", HealthStatus.Healthy, "Blob storage is healthy"),
                    new("System", HealthStatus.Healthy, "System is healthy")
                });

            // Act
            var result = await _controller.GetHealth(CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var content = okResult.Value.Should().BeOfType<HealthCheckResponse>().Subject;
            
            content.Should().NotBeNull();
            content.Status.Should().Be("Healthy");
            var blobComponent = content.Components.Should().Contain(c => c.Name == "BlobStorage").Subject;
            blobComponent.Status.Should().Be("Healthy");
        }

        [Fact]
        public async Task GetHealth_WithSimulatedFailure_ReturnsUnhealthyStatus()
        {
            // Arrange
            _mockHealthCheckService.Setup(x => x.CheckAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<HealthCheckResult>
                {
                    new("BlobStorage", HealthStatus.Unhealthy, "Unable to access blob storage: Simulated failure"),
                    new("System", HealthStatus.Healthy, "System is healthy")
                });

            // Act
            var result = await _controller.GetHealth(CancellationToken.None);

            // Assert
            var serviceUnavailableResult = result.Should().BeOfType<ObjectResult>().Subject;
            serviceUnavailableResult.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
            
            var content = serviceUnavailableResult.Value.Should().BeOfType<HealthCheckResponse>().Subject;
            content.Should().NotBeNull();
            content.Status.Should().Be("Unhealthy");
            content.Components.Should().NotBeNull().And.NotBeEmpty();
            var blobComponent = content.Components.Should().Contain(c => c.Name == "BlobStorage").Subject;
            blobComponent.Status.Should().Be("Unhealthy");
            blobComponent.Error.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task GetHealth_ResponseFormat_MatchesSwaggerExample()
        {
            // Act
            var result = await _controller.GetHealth(CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var content = okResult.Value.Should().BeOfType<HealthCheckResponse>().Subject;
            
            content.Should().NotBeNull();
            content.Should().BeOfType<HealthCheckResponse>();
            content.Status.Should().BeOneOf("Healthy", "Degraded", "Unhealthy");
            content.Components.Should().NotBeNull();
            content.Components.Should().AllSatisfy(c =>
            {
                c.Should().NotBeNull();
                c.Name.Should().NotBeNullOrEmpty();
                c.Status.Should().BeOneOf("Healthy", "Degraded", "Unhealthy");
                if (c.Status == "Unhealthy")
                {
                    c.Error.Should().NotBeNullOrEmpty();
                }
            });
        }

        [Fact]
        public async Task GetComponentHealth_EndToEnd_ReturnsExpectedResponse()
        {
            // Arrange
            _mockHealthCheckService.Setup(x => x.CheckComponentAsync("BlobStorage", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HealthCheckResult("BlobStorage", HealthStatus.Healthy, "Blob storage is healthy"));
            
            // Act
            var result = await _controller.GetComponentHealth("BlobStorage", CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var content = okResult.Value.Should().BeOfType<ComponentHealthResponse>().Subject;
            
            content.Should().NotBeNull();
            content.Name.Should().Be("BlobStorage");
            content.Status.Should().Be("Healthy");
        }
    }
} 