using EventStore.Application.Health.Responses;
using Swashbuckle.AspNetCore.Filters;

namespace EventStore.Api.Examples;

/// <summary>
/// Provides an example of a healthy system response where all components are functioning normally.
/// </summary>
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

/// <summary>
/// Provides an example of a degraded system response where some components are experiencing issues but are still operational.
/// </summary>
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

/// <summary>
/// Provides an example of an unhealthy system response where at least one component is not functioning correctly.
/// </summary>
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

/// <summary>
/// Provides an example of a healthy component-specific response.
/// </summary>
public class HealthyComponentResponseExample : IExamplesProvider<ComponentHealthResponse>
{
    public ComponentHealthResponse GetExamples()
    {
        return new ComponentHealthResponse(
            "BlobStorage",
            "Healthy",
            "Blob storage is accessible with 150ms response time",
            null);
    }
}

/// <summary>
/// Provides an example of an unhealthy component-specific response.
/// </summary>
public class UnhealthyComponentResponseExample : IExamplesProvider<ComponentHealthResponse>
{
    public ComponentHealthResponse GetExamples()
    {
        return new ComponentHealthResponse(
            "BlobStorage",
            "Unhealthy",
            null,
            "Failed to connect to blob storage: Connection timeout after 5000ms");
    }
} 