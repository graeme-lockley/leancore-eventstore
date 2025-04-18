using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EventStore.Domain.Health;

namespace EventStore.Infrastructure.Health;

/// <summary>
/// Health check implementation for system metrics
/// </summary>
public class SystemHealthCheck : IHealthCheck
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

    public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var metrics = await Task.Run(() => CollectMetrics(), cancellationToken);
            var status = DetermineHealthStatus(metrics);

            _logger.LogDebug("System health check completed. Status: {Status}", status);

            return new HealthCheckResult(
                ComponentName,
                status,
                status switch
                {
                    HealthStatus.Healthy => "System is operating normally",
                    HealthStatus.Degraded => "System is experiencing high resource usage",
                    HealthStatus.Unhealthy => "System is experiencing critical resource issues",
                    _ => throw new ArgumentOutOfRangeException(nameof(status))
                },
                metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "System health check failed");
            return new HealthCheckResult(
                ComponentName,
                HealthStatus.Unhealthy,
                "Failed to collect system metrics",
                new Dictionary<string, object>
                {
                    { "error", ex.Message },
                    { "errorType", ex.GetType().Name }
                });
        }
    }

    private Dictionary<string, object> CollectMetrics()
    {
        var metrics = new Dictionary<string, object>();

        // Memory metrics
        var workingSet = _currentProcess.WorkingSet64;
        var privateMemory = _currentProcess.PrivateMemorySize64;
        var managedMemory = GC.GetTotalMemory(false);

        metrics.Add("workingSetBytes", workingSet);
        metrics.Add("privateMemoryBytes", privateMemory);
        metrics.Add("managedMemoryBytes", managedMemory);
        metrics.Add("gcCollectionCount", new Dictionary<string, int>
        {
            { "gen0", GC.CollectionCount(0) },
            { "gen1", GC.CollectionCount(1) },
            { "gen2", GC.CollectionCount(2) }
        });

        // Thread pool metrics
        ThreadPool.GetAvailableThreads(out int workerThreads, out int completionPortThreads);
        ThreadPool.GetMaxThreads(out int maxWorkerThreads, out int maxCompletionPortThreads);

        metrics.Add("threadPool", new Dictionary<string, object>
        {
            { "availableWorkerThreads", workerThreads },
            { "availableIoThreads", completionPortThreads },
            { "maxWorkerThreads", maxWorkerThreads },
            { "maxIoThreads", maxCompletionPortThreads }
        });

        // CPU metrics
        metrics.Add("cpuTime", new Dictionary<string, TimeSpan>
        {
            { "total", _currentProcess.TotalProcessorTime },
            { "user", _currentProcess.UserProcessorTime },
            { "privileged", _currentProcess.PrivilegedProcessorTime }
        });

        // Process metrics
        metrics.Add("handles", _currentProcess.HandleCount);
        metrics.Add("threads", _currentProcess.Threads.Count);
        metrics.Add("startTime", _currentProcess.StartTime.ToUniversalTime());
        metrics.Add("uptime", DateTime.UtcNow - _currentProcess.StartTime.ToUniversalTime());

        return metrics;
    }

    private HealthStatus DetermineHealthStatus(Dictionary<string, object> metrics)
    {
        // Check memory thresholds
        var workingSetBytes = (long)metrics["workingSetBytes"];
        if (workingSetBytes > _options.MemoryThresholds.UnhealthyBytes)
            return HealthStatus.Unhealthy;
        if (workingSetBytes > _options.MemoryThresholds.DegradedBytes)
            return HealthStatus.Degraded;

        // Check thread pool utilization
        var threadPool = (Dictionary<string, object>)metrics["threadPool"];
        var availableWorkerThreads = (int)threadPool["availableWorkerThreads"];
        var maxWorkerThreads = (int)threadPool["maxWorkerThreads"];
        var threadPoolUtilization = 1 - ((double)availableWorkerThreads / maxWorkerThreads);

        if (threadPoolUtilization > _options.ThreadPoolThresholds.UnhealthyUtilization)
            return HealthStatus.Unhealthy;
        if (threadPoolUtilization > _options.ThreadPoolThresholds.DegradedUtilization)
            return HealthStatus.Degraded;

        return HealthStatus.Healthy;
    }
} 