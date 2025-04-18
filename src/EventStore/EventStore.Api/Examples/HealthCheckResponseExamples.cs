using EventStore.Application.Health.Responses;
using Swashbuckle.AspNetCore.Filters;

namespace EventStore.Api.Examples;

public class HealthyResponseExample : IExamplesProvider<HealthCheckResponse>
{
    public HealthCheckResponse GetExamples()
    {
        return new HealthCheckResponse(
            "Healthy",
            DateTimeOffset.UtcNow,
            new List<ComponentHealthResponse>
            {
                new(
                    "BlobStorage",
                    "Healthy",
                    "Blob storage is accessible",
                    null),
                new(
                    "System",
                    "Healthy",
                    "System is operating normally",
                    null)
            });
    }
}

public class DegradedResponseExample : IExamplesProvider<HealthCheckResponse>
{
    public HealthCheckResponse GetExamples()
    {
        return new HealthCheckResponse(
            "Degraded",
            DateTimeOffset.UtcNow,
            new List<ComponentHealthResponse>
            {
                new(
                    "BlobStorage",
                    "Healthy",
                    "Blob storage is accessible",
                    null),
                new(
                    "System",
                    "Degraded",
                    "System is experiencing high resource usage",
                    null)
            });
    }
}

public class UnhealthyResponseExample : IExamplesProvider<HealthCheckResponse>
{
    public HealthCheckResponse GetExamples()
    {
        return new HealthCheckResponse(
            "Unhealthy",
            DateTimeOffset.UtcNow,
            new List<ComponentHealthResponse>
            {
                new(
                    "BlobStorage",
                    "Unhealthy",
                    null,
                    "Failed to connect to blob storage"),
                new(
                    "System",
                    "Healthy",
                    "System is operating normally",
                    null)
            });
    }
} 