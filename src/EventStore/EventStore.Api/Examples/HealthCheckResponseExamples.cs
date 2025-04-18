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
            new[]
            {
                new ComponentHealthResponse(
                    "System",
                    "Healthy",
                    "System is operating normally",
                    null,
                    new Dictionary<string, object>
                    {
                        { "workingSetBytes", 104857600 }, // 100 MB
                        { "privateMemoryBytes", 157286400 }, // 150 MB
                        { "managedMemoryBytes", 52428800 }, // 50 MB
                        { "gcCollectionCount", new Dictionary<string, int>
                            {
                                { "gen0", 100 },
                                { "gen1", 10 },
                                { "gen2", 1 }
                            }
                        },
                        { "threadPool", new Dictionary<string, object>
                            {
                                { "availableWorkerThreads", 80 },
                                { "availableIoThreads", 90 },
                                { "maxWorkerThreads", 100 },
                                { "maxIoThreads", 100 }
                            }
                        }
                    }),
                new ComponentHealthResponse(
                    "BlobStorage",
                    "Healthy",
                    "Blob storage is accessible",
                    null,
                    new Dictionary<string, object>
                    {
                        { "responseTime", "50ms" },
                        { "accountName", "mystorageaccount" },
                        { "defaultServiceVersion", "2020-10-02" },
                        { "staticWebsiteEnabled", false }
                    })
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
            new[]
            {
                new ComponentHealthResponse(
                    "System",
                    "Degraded",
                    "System is experiencing high resource usage",
                    null,
                    new Dictionary<string, object>
                    {
                        { "workingSetBytes", 1073741824 }, // 1 GB
                        { "privateMemoryBytes", 1610612736 }, // 1.5 GB
                        { "managedMemoryBytes", 536870912 }, // 512 MB
                        { "gcCollectionCount", new Dictionary<string, int>
                            {
                                { "gen0", 500 },
                                { "gen1", 50 },
                                { "gen2", 5 }
                            }
                        },
                        { "threadPool", new Dictionary<string, object>
                            {
                                { "availableWorkerThreads", 25 },
                                { "availableIoThreads", 30 },
                                { "maxWorkerThreads", 100 },
                                { "maxIoThreads", 100 }
                            }
                        }
                    }),
                new ComponentHealthResponse(
                    "BlobStorage",
                    "Healthy",
                    "Blob storage is accessible but with high latency",
                    null,
                    new Dictionary<string, object>
                    {
                        { "responseTime", "450ms" },
                        { "accountName", "mystorageaccount" },
                        { "defaultServiceVersion", "2020-10-02" },
                        { "staticWebsiteEnabled", false }
                    })
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
            new[]
            {
                new ComponentHealthResponse(
                    "System",
                    "Unhealthy",
                    null,
                    "High memory usage detected",
                    new Dictionary<string, object>
                    {
                        { "workingSetBytes", 2147483648 }, // 2 GB
                        { "privateMemoryBytes", 3221225472 }, // 3 GB
                        { "managedMemoryBytes", 1073741824 }, // 1 GB
                        { "gcCollectionCount", new Dictionary<string, int>
                            {
                                { "gen0", 1000 },
                                { "gen1", 100 },
                                { "gen2", 10 }
                            }
                        },
                        { "threadPool", new Dictionary<string, object>
                            {
                                { "availableWorkerThreads", 5 },
                                { "availableIoThreads", 10 },
                                { "maxWorkerThreads", 100 },
                                { "maxIoThreads", 100 }
                            }
                        }
                    }),
                new ComponentHealthResponse(
                    "BlobStorage",
                    "Unhealthy",
                    null,
                    "Failed to connect to blob storage",
                    new Dictionary<string, object>
                    {
                        { "error", "Connection timeout" },
                        { "timeoutMs", 5000 }
                    })
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
            "Blob storage is accessible",
            null,
            new Dictionary<string, object>
            {
                { "responseTime", "50ms" },
                { "accountName", "mystorageaccount" },
                { "defaultServiceVersion", "2020-10-02" },
                { "staticWebsiteEnabled", false }
            });
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
            "Failed to connect to blob storage",
            new Dictionary<string, object>
            {
                { "error", "Connection timeout" },
                { "timeoutMs", 5000 }
            });
    }
} 