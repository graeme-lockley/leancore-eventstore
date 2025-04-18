using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EventStore.Domain.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace EventStore.Infrastructure.Health;

/// <summary>
/// Health check implementation for system metrics
/// </summary>
public class SystemHealthCheck : EventStore.Domain.Health.IHealthCheck,
    Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
{
    private readonly ILogger<SystemHealthCheck> _logger;
    private readonly SystemHealthCheckOptions _options;
    private readonly Process _currentProcess;

    public string ComponentName => "System";

    public SystemHealthCheck(
        ILogger<SystemHealthCheck> logger,
        IOptions<SystemHealthCheckOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _currentProcess = Process.GetCurrentProcess();
    }

    public async Task<EventStore.Domain.Health.HealthCheckResult> CheckHealthAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await CheckHealthInternalAsync(cancellationToken);
        return new EventStore.Domain.Health.HealthCheckResult(
            ComponentName,
            result.Status switch
            {
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy =>
                    Domain.Health.HealthStatus.Healthy,
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded => Domain.Health.HealthStatus
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

            ThreadPool.GetAvailableThreads(out int workerThreads, out int completionPortThreads);
            ThreadPool.GetMaxThreads(out int maxWorkerThreads, out int maxCompletionPortThreads);

            var threadPoolUsagePercentage = (1 - (double)workerThreads / maxWorkerThreads) * 100;

            if (_options.IncludeDetailedInfo)
            {
                data.Add("memoryUsagePercentage", Math.Round(memoryUsagePercentage, 2));
                data.Add("threadPoolUsagePercentage", Math.Round(threadPoolUsagePercentage, 2));
            }

            var status = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy;
            var description = "System resources are within acceptable limits";

            if (usedMemory > _options.MemoryThresholds.UnhealthyBytes)
            {
                status = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy;
                description = $"Memory usage ({Math.Round(memoryUsagePercentage, 2)}%) exceeds unhealthy threshold";
            }
            else if (usedMemory > _options.MemoryThresholds.DegradedBytes)
            {
                status = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded;
                description = $"Memory usage ({Math.Round(memoryUsagePercentage, 2)}%) exceeds degraded threshold";
            }

            if (threadPoolUsagePercentage > _options.ThreadPoolThresholds.UnhealthyUtilization * 100)
            {
                status = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy;
                description =
                    $"Thread pool usage ({Math.Round(threadPoolUsagePercentage, 2)}%) exceeds unhealthy threshold";
            }
            else if (threadPoolUsagePercentage > _options.ThreadPoolThresholds.DegradedUtilization * 100)
            {
                status = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded;
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
                    Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
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
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
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