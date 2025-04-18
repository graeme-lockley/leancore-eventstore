using System.Net;
using System.Net.Http.Json;
using Azure.Storage.Blobs;
using EventStore.Api;
using EventStore.Application.Health.Responses;
using EventStore.Domain.Health;
using EventStore.Infrastructure.Health;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;
using System.Text.Json;
using MicrosoftHealthChecks = Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EventStore.Tests.Api.Controllers;

public class HealthControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<BlobServiceClient> _mockBlobServiceClient;

    public HealthControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _mockBlobServiceClient = new Mock<BlobServiceClient>();
        
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                var inMemorySettings = new Dictionary<string, string>
                {
                    {"AzureStorage:ConnectionString", "UseDevelopmentStorage=true"}
                };

                config.AddInMemoryCollection(inMemorySettings);
            });

            builder.ConfigureServices(services =>
            {
                // Remove the real BlobServiceClient registration
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(BlobServiceClient));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add the mock BlobServiceClient
                services.AddSingleton(_mockBlobServiceClient.Object);
            });
        });
    }

    [Fact]
    public async Task GetHealth_EndToEnd_ReturnsExpectedResponse()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/health");
        var content = await response.Content.ReadFromJsonAsync<HealthCheckResponse>();

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
        content.Should().NotBeNull();
        content!.Components.Should().NotBeEmpty();
        content.Components.Should().Contain(c => c.Name == "BlobStorage");
        content.Components.Should().Contain(c => c.Name == "System");
    }

    [Fact]
    public async Task GetHealth_WithActualBlobStorage_ReturnsStorageStatus()
    {
        // Arrange
        _mockBlobServiceClient.Setup(x => x.GetBlobContainerClient(It.IsAny<string>()))
            .Returns(Mock.Of<BlobContainerClient>());

        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/health");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var healthResponse = JsonSerializer.Deserialize<HealthCheckResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(healthResponse);
        Assert.Contains(healthResponse.Components, c => c.Name == "BlobStorage");
    }

    [Fact]
    public async Task GetHealth_WithSimulatedFailure_ReturnsUnhealthyStatus()
    {
        // Arrange
        _mockBlobServiceClient.Setup(x => x.GetPropertiesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Simulated failure"));

        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var content = await response.Content.ReadAsStringAsync();
        var healthResponse = JsonSerializer.Deserialize<HealthCheckResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(healthResponse);
        var storageComponent = Assert.Single(healthResponse.Components.Where(c => c.Name == "BlobStorage"));
        Assert.Equal(HealthStatus.Unhealthy.ToString(), storageComponent.Status);
    }

    [Fact]
    public async Task GetHealth_ResponseFormat_MatchesSwaggerExample()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/health");
        var content = await response.Content.ReadFromJsonAsync<HealthCheckResponse>();

        // Assert
        content.Should().NotBeNull();
        content!.Should().BeOfType<HealthCheckResponse>();
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
                c.Details.Should().BeNull();
            }
            else
            {
                c.Error.Should().BeNull();
                c.Details.Should().NotBeNullOrEmpty();
            }
        });
    }

    [Fact]
    public async Task GetComponentHealth_EndToEnd_ReturnsExpectedResponse()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/health/BlobStorage");
        var content = await response.Content.ReadFromJsonAsync<ComponentHealthResponse>();

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
        content.Should().NotBeNull();
        content!.Name.Should().Be("BlobStorage");
        content.Status.Should().BeOneOf("Healthy", "Unhealthy");
    }
}

public class MockUnhealthyCheck : MicrosoftHealthChecks.IHealthCheck
{
    private readonly string _componentName;

    public MockUnhealthyCheck(string componentName)
    {
        _componentName = componentName;
    }

    public string ComponentName => _componentName;

    public Task<MicrosoftHealthChecks.HealthCheckResult> CheckHealthAsync(
        MicrosoftHealthChecks.HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(MicrosoftHealthChecks.HealthCheckResult.Unhealthy(
            "Simulated failure for testing",
            data: new Dictionary<string, object>()));
    }
} 