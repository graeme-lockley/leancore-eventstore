using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EventStore.Domain.Health;

namespace EventStore.Infrastructure.Health;

/// <summary>
/// Implements a health check for monitoring system metrics including memory usage, thread pool status, and CPU utilization.
/// This health check provides detailed information about the current state of the application process and system resources.
/// </summary>
public class SystemHealthCheck : IHealthCheck
{
    private readonly ILogger<SystemHealthCheck> _logger;
    private readonly SystemHealthCheckOptions _options;
    private readonly Process _currentProcess;

    /// <inheritdoc/>
    public string ComponentName => "System";

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemHealthCheck"/> class.
    /// </summary>
    /// <param name="logger">The logger for recording health check activities.</param>
    /// <param name="options">Configuration options for system health thresholds.</param>
    /// <exception cref="ArgumentNullException">Thrown when logger or options is null.</exception>
    public SystemHealthCheck(
        ILogger<SystemHealthCheck> logger,
        IOptions<SystemHealthCheckOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _currentProcess = Process.GetCurrentProcess();
    }

    /// <summary>
    /// Performs a health check by gathering and analyzing system metrics.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="HealthCheckResult"/> containing:
    /// - Status: Based on memory usage and thread pool utilization thresholds
    /// - Description: Summary of the system's health
    /// - Data: Detailed metrics including:
    ///   * Memory usage (working set, private memory, managed memory)
    ///   * GC collection counts
    ///   * Thread pool statistics
    ///   * CPU utilization
    ///   * Process information (handles, threads, uptime)
    /// </returns>
    public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var metrics = await CollectMetricsAsync();
            var status = DetermineHealthStatus(metrics);
            var description = GenerateDescription(status, metrics);

            return new HealthCheckResult(
                ComponentName,
                status,
                description,
                metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting system metrics");
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

    /// <summary>
    /// Collects various system metrics asynchronously.
    /// </summary>
    /// <returns>A dictionary containing the collected metrics.</returns>
    private async Task<Dictionary<string, object>> CollectMetricsAsync()
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

    /// <summary>
    /// Determines the overall health status based on the collected metrics.
    /// </summary>
    /// <param name="metrics">The collected system metrics.</param>
    /// <returns>The determined health status (Healthy, Degraded, or Unhealthy).</returns>
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

    /// <summary>
    /// Generates a human-readable description of the system health status.
    /// </summary>
    /// <param name="status">The determined health status.</param>
    /// <param name="metrics">The collected system metrics.</param>
    /// <returns>A description string explaining the current system health.</returns>
    private string GenerateDescription(HealthStatus status, Dictionary<string, object> metrics)
    {
        return status switch
        {
            HealthStatus.Healthy => "System is operating normally",
            HealthStatus.Degraded => "System is experiencing high resource usage",
            HealthStatus.Unhealthy => "System is experiencing critical resource issues",
            _ => throw new ArgumentOutOfRangeException(nameof(status))
        };
    }
} 