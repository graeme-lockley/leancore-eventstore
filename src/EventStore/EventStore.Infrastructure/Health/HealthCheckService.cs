using System.Collections.Concurrent;
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

    public HealthCheckService(
        ILogger<HealthCheckService> logger,
        IOptions<HealthCheckServiceOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _healthChecks = new ConcurrentDictionary<string, IHealthCheck>();
        _cache = new ConcurrentDictionary<string, (HealthCheckResult, DateTimeOffset)>();
    }

    public void RegisterHealthCheck(IHealthCheck healthCheck)
    {
        if (healthCheck == null) throw new ArgumentNullException(nameof(healthCheck));

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
            _logger.LogDebug("Returning cached health check result for component: {ComponentName}", componentName);
            return cachedResult;
        }

        try
        {
            var result = await healthCheck.CheckHealthAsync(cancellationToken);
            
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
            _logger.LogError(ex, "Health check failed for component: {ComponentName}", componentName);
            return new HealthCheckResult(
                componentName,
                HealthStatus.Unhealthy,
                $"Health check failed: {ex.Message}");
        }
    }

    public async Task<IReadOnlyCollection<HealthCheckResult>> CheckAllAsync(CancellationToken cancellationToken = default)
    {
        var tasks = _healthChecks.Values.Select(hc => CheckComponentAsync(hc.ComponentName, cancellationToken));
        var results = await Task.WhenAll(tasks);
        
        _logger.LogInformation(
            "Completed health checks for {Count} components. " +
            "Healthy: {Healthy}, Degraded: {Degraded}, Unhealthy: {Unhealthy}",
            results.Length,
            results.Count(r => r.Status == HealthStatus.Healthy),
            results.Count(r => r.Status == HealthStatus.Degraded),
            results.Count(r => r.Status == HealthStatus.Unhealthy));

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
            _logger.LogDebug("Cached result expired for component: {ComponentName}", componentName);
            return false;
        }

        result = cached.Result;
        return true;
    }
} 