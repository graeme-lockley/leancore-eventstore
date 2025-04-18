using System.Collections.Concurrent;
using System.Diagnostics;
using EventStore.Domain.Health;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventStore.Infrastructure.Health;

/// <summary>
/// Service for managing and executing health checks
/// </summary>
public class HealthCheckService : IHealthCheckService
{
    private readonly ConcurrentDictionary<string, IHealthCheck> _healthChecks;
    private readonly ILogger<HealthCheckService> _logger;
    private readonly HealthCheckServiceOptions _options;
    private readonly ConcurrentDictionary<string, (HealthCheckResult Result, DateTimeOffset Timestamp)> _cache;
    private readonly ConcurrentDictionary<string, HealthStatus> _lastKnownStatus;

    public HealthCheckService(
        ILogger<HealthCheckService> logger,
        IOptions<HealthCheckServiceOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _healthChecks = new ConcurrentDictionary<string, IHealthCheck>();
        _cache = new ConcurrentDictionary<string, (HealthCheckResult, DateTimeOffset)>();
        _lastKnownStatus = new ConcurrentDictionary<string, HealthStatus>();
    }

    public void RegisterHealthCheck(IHealthCheck healthCheck)
    {
        if (healthCheck == null) throw new ArgumentNullException(nameof(healthCheck));

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = Guid.NewGuid(),
            ["Operation"] = "RegisterHealthCheck",
            ["ComponentName"] = healthCheck.ComponentName
        });

        if (_healthChecks.TryAdd(healthCheck.ComponentName, healthCheck))
        {
            _logger.LogInformation("Registered health check for component: {ComponentName}", healthCheck.ComponentName);
        }
        else
        {
            _logger.LogWarning("Health check for component {ComponentName} is already registered", healthCheck.ComponentName);
        }
    }

    public async Task<HealthCheckResult> CheckComponentAsync(string componentName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(componentName))
            throw new ArgumentException("Component name cannot be null or empty", nameof(componentName));

        var correlationId = Guid.NewGuid();
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["Operation"] = "CheckComponent",
            ["ComponentName"] = componentName
        });

        if (!_healthChecks.TryGetValue(componentName, out var healthCheck))
        {
            _logger.LogWarning("No health check registered for component: {ComponentName}", componentName);
            return new HealthCheckResult(
                componentName,
                HealthStatus.Unhealthy,
                $"No health check registered for component: {componentName}");
        }

        // Check cache if enabled
        if (_options.EnableCaching && TryGetCachedResult(componentName, out var cachedResult))
        {
            _logger.LogDebug(
                "Returning cached health check result for component: {ComponentName}, Status: {Status}, Age: {CacheAge}ms",
                componentName,
                cachedResult.Status,
                (DateTimeOffset.UtcNow - _cache[componentName].Timestamp).TotalMilliseconds);
            return cachedResult;
        }

        try
        {
            var stopwatch = Stopwatch.StartNew();
            var result = await healthCheck.CheckHealthAsync(cancellationToken);
            stopwatch.Stop();

            // Log status changes
            if (_lastKnownStatus.TryGetValue(componentName, out var lastStatus) && lastStatus != result.Status)
            {
                _logger.LogInformation(
                    "Component {ComponentName} status changed from {PreviousStatus} to {CurrentStatus}",
                    componentName,
                    lastStatus,
                    result.Status);
            }
            _lastKnownStatus.AddOrUpdate(componentName, result.Status, (_, _) => result.Status);

            _logger.LogInformation(
                "Health check completed for component {ComponentName}. Status: {Status}, Duration: {Duration}ms",
                componentName,
                result.Status,
                stopwatch.ElapsedMilliseconds);

            // Cache the result if enabled
            if (_options.EnableCaching)
            {
                _cache.AddOrUpdate(
                    componentName,
                    (result, DateTimeOffset.UtcNow),
                    (_, _) => (result, DateTimeOffset.UtcNow));
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Health check failed for component: {ComponentName}, CorrelationId: {CorrelationId}",
                componentName,
                correlationId);

            var errorResult = new HealthCheckResult(
                componentName,
                HealthStatus.Unhealthy,
                $"Health check failed: {ex.Message}");

            // Log status change to unhealthy
            if (_lastKnownStatus.TryGetValue(componentName, out var lastStatus) && lastStatus != HealthStatus.Unhealthy)
            {
                _logger.LogInformation(
                    "Component {ComponentName} status changed from {PreviousStatus} to Unhealthy due to error",
                    componentName,
                    lastStatus);
            }
            _lastKnownStatus.AddOrUpdate(componentName, HealthStatus.Unhealthy, (_, _) => HealthStatus.Unhealthy);

            return errorResult;
        }
    }

    public async Task<IReadOnlyCollection<HealthCheckResult>> CheckAllAsync(CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid();
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["Operation"] = "CheckAll"
        });

        _logger.LogInformation(
            "Starting health checks for {Count} components",
            _healthChecks.Count);

        var stopwatch = Stopwatch.StartNew();
        var tasks = _healthChecks.Values.Select(hc => CheckComponentAsync(hc.ComponentName, cancellationToken));
        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        var healthy = results.Count(r => r.Status == HealthStatus.Healthy);
        var degraded = results.Count(r => r.Status == HealthStatus.Degraded);
        var unhealthy = results.Count(r => r.Status == HealthStatus.Unhealthy);

        _logger.LogInformation(
            "Completed health checks for {Count} components in {Duration}ms. " +
            "Healthy: {Healthy}, Degraded: {Degraded}, Unhealthy: {Unhealthy}",
            results.Length,
            stopwatch.ElapsedMilliseconds,
            healthy,
            degraded,
            unhealthy);

        if (unhealthy > 0)
        {
            _logger.LogWarning(
                "Unhealthy components detected: {UnhealthyComponents}",
                string.Join(", ", results.Where(r => r.Status == HealthStatus.Unhealthy).Select(r => r.ComponentName)));
        }

        return results;
    }

    private bool TryGetCachedResult(string componentName, out HealthCheckResult result)
    {
        result = null!;

        if (!_cache.TryGetValue(componentName, out var cached))
            return false;

        var age = DateTimeOffset.UtcNow - cached.Timestamp;
        if (age > _options.CacheDuration)
        {
            _logger.LogDebug(
                "Cached result expired for component: {ComponentName}, Age: {CacheAge}ms",
                componentName,
                age.TotalMilliseconds);
            return false;
        }

        result = cached.Result;
        return true;
    }
} 