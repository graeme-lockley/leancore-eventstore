using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using HealthCheckResult = EventStore.Domain.Health.HealthCheckResult;

namespace EventStore.Infrastructure.Health;

/// <summary>
/// Health check implementation for system metrics
/// </summary>
public class SystemHealthCheck(
    ILogger<SystemHealthCheck> logger,
    IOptions<SystemHealthCheckOptions> options)
    : EventStore.Domain.Health.IHealthCheck,
        IHealthCheck
{
    private readonly ILogger<SystemHealthCheck> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly SystemHealthCheckOptions _options = options.Value ?? throw new ArgumentNullException(nameof(options));

    public string ComponentName => "System";

    public async Task<HealthCheckResult> CheckHealthAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await CheckHealthInternalAsync(cancellationToken);
        return new HealthCheckResult(
            ComponentName,
            result.Status switch
            {
                HealthStatus.Healthy =>
                    Domain.Health.HealthStatus.Healthy,
                HealthStatus.Degraded => Domain.Health.HealthStatus
                    .Degraded,
                _ => Domain.Health.HealthStatus.Unhealthy
            },
            result.Description ?? string.Empty,
            result.Data.ToDictionary(x => x.Key, x => x.Value));
    }

    public Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        return CheckHealthInternalAsync(cancellationToken);
    }

    private async Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthInternalAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var data = new Dictionary<string, object>();

            var memoryInfo = GC.GetGCMemoryInfo();
            var totalMemory = memoryInfo.TotalAvailableMemoryBytes;
            var usedMemory = GC.GetTotalMemory(false);
            var memoryUsagePercentage = (double)usedMemory / totalMemory * 100;

            ThreadPool.GetAvailableThreads(out var workerThreads, out _);
            ThreadPool.GetMaxThreads(out var maxWorkerThreads, out _);

            var threadPoolUsagePercentage = (1 - (double)workerThreads / maxWorkerThreads) * 100;

            if (_options.IncludeDetailedInfo)
            {
                data.Add("memoryUsagePercentage", Math.Round(memoryUsagePercentage, 2));
                data.Add("threadPoolUsagePercentage", Math.Round(threadPoolUsagePercentage, 2));
            }

            var status = HealthStatus.Healthy;
            var description = "System resources are within acceptable limits";

            if (usedMemory > _options.MemoryThresholds.UnhealthyBytes)
            {
                status = HealthStatus.Unhealthy;
                description = $"Memory usage ({Math.Round(memoryUsagePercentage, 2)}%) exceeds unhealthy threshold";
            }
            else if (usedMemory > _options.MemoryThresholds.DegradedBytes)
            {
                status = HealthStatus.Degraded;
                description = $"Memory usage ({Math.Round(memoryUsagePercentage, 2)}%) exceeds degraded threshold";
            }

            if (threadPoolUsagePercentage > _options.ThreadPoolThresholds.UnhealthyUtilization * 100)
            {
                status = HealthStatus.Unhealthy;
                description =
                    $"Thread pool usage ({Math.Round(threadPoolUsagePercentage, 2)}%) exceeds unhealthy threshold";
            }
            else if (threadPoolUsagePercentage > _options.ThreadPoolThresholds.DegradedUtilization * 100)
            {
                status = HealthStatus.Degraded;
                description =
                    $"Thread pool usage ({Math.Round(threadPoolUsagePercentage, 2)}%) exceeds degraded threshold";
            }

            try
            {
                _logger.LogInformation("System health check completed. Status: {Status}", status);
            }
            catch (Exception ex)
            {
                return new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult(
                    HealthStatus.Unhealthy,
                    "Error checking system health",
                    ex,
                    _options.IncludeDetailedInfo
                        ? new Dictionary<string, object>
                        {
                            { "Error", ex.Message }
                        }
                        : null);
            }

            
            return new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult(
                status,
                description,
                null,
                data);
        }
        catch (Exception ex)
        {
            try
            {
                _logger.LogError(ex, "Error checking system health");
            }
            catch
            {
                // Ignore logging errors
            }

            return new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult(
                HealthStatus.Unhealthy,
                "Error checking system health",
                ex,
                _options.IncludeDetailedInfo
                    ? new Dictionary<string, object>
                    {
                        { "Error", ex.Message }
                    }
                    : null);
        }
    }
}