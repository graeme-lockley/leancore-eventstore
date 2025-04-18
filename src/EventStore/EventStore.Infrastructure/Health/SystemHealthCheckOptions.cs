namespace EventStore.Infrastructure.Health;

/// <summary>
/// Configuration options for the system health check
/// </summary>
public class SystemHealthCheckOptions
{
    /// <summary>
    /// Gets or sets the memory thresholds for determining system health
    /// </summary>
    public MemoryThresholds MemoryThresholds { get; set; } = new();

    /// <summary>
    /// Gets or sets the thread pool thresholds for determining system health
    /// </summary>
    public ThreadPoolThresholds ThreadPoolThresholds { get; set; } = new();
}

/// <summary>
/// Memory thresholds for determining system health
/// </summary>
public class MemoryThresholds
{
    /// <summary>
    /// Gets or sets the memory threshold in bytes for degraded status
    /// </summary>
    public long DegradedBytes { get; set; } = 1024L * 1024L * 1024L; // 1 GB

    /// <summary>
    /// Gets or sets the memory threshold in bytes for unhealthy status
    /// </summary>
    public long UnhealthyBytes { get; set; } = 2L * 1024L * 1024L * 1024L; // 2 GB
}

/// <summary>
/// Thread pool thresholds for determining system health
/// </summary>
public class ThreadPoolThresholds
{
    /// <summary>
    /// Gets or sets the thread pool utilization threshold for degraded status (0.0 to 1.0)
    /// </summary>
    public double DegradedUtilization { get; set; } = 0.8; // 80%

    /// <summary>
    /// Gets or sets the thread pool utilization threshold for unhealthy status (0.0 to 1.0)
    /// </summary>
    public double UnhealthyUtilization { get; set; } = 0.9; // 90%
} 