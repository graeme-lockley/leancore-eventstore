using Azure.Storage.Blobs;
using EventStore.Domain.Health;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventStore.Infrastructure.Health;

/// <summary>
/// Health check implementation for Azure Blob Storage
/// </summary>
public class BlobStorageHealthCheck : IHealthCheck
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<BlobStorageHealthCheck> _logger;
    private readonly BlobStorageHealthCheckOptions _options;

    public string ComponentName => "Azure Blob Storage";

    public BlobStorageHealthCheck(
        BlobServiceClient blobServiceClient,
        ILogger<BlobStorageHealthCheck> logger,
        IOptions<BlobStorageHealthCheckOptions> options)
    {
        _blobServiceClient = blobServiceClient ?? throw new ArgumentNullException(nameof(blobServiceClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var startTime = DateTimeOffset.UtcNow;
            var properties = await _blobServiceClient.GetPropertiesAsync(cancellationToken);
            var responseTime = DateTimeOffset.UtcNow - startTime;

            _logger.LogDebug("Blob storage health check completed in {ResponseTime}ms", responseTime.TotalMilliseconds);

            var data = new Dictionary<string, object>
            {
                { "responseTime", $"{responseTime.TotalMilliseconds}ms" },
                { "accountName", _blobServiceClient.AccountName }
            };

            if (_options.IncludeDetailedInfo)
            {
                data["defaultServiceVersion"] = properties.Value.DefaultServiceVersion;
                data["staticWebsiteEnabled"] = properties.Value.StaticWebsite?.Enabled ?? false;
            }

            return new HealthCheckResult(
                ComponentName,
                HealthStatus.Healthy,
                "Blob storage is accessible",
                data);
        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException)
        {
            _logger.LogWarning("Blob storage health check timed out after {Timeout}ms", _options.TimeoutMs);
            
            return new HealthCheckResult(
                ComponentName,
                HealthStatus.Degraded,
                "Health check timed out",
                new Dictionary<string, object>
                {
                    { "error", "Timeout" },
                    { "timeoutMs", _options.TimeoutMs }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Blob storage health check failed");
            
            return new HealthCheckResult(
                ComponentName,
                HealthStatus.Unhealthy,
                "Failed to connect to blob storage",
                new Dictionary<string, object>
                {
                    { "error", ex.Message },
                    { "errorType", ex.GetType().Name }
                });
        }
    }
} 