using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EventStore.Domain.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EventStore.Infrastructure.Health;

public interface IBlobServiceClient
{
    string AccountName { get; }
    Task<Response<BlobServiceProperties>> GetPropertiesAsync(CancellationToken cancellationToken = default);
}

public class BlobServiceClientWrapper : IBlobServiceClient
{
    private readonly BlobServiceClient _client;

    public BlobServiceClientWrapper(BlobServiceClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public string AccountName
    {
        get
        {
            try
            {
                return _client.AccountName;
            }
            catch (ArgumentNullException)
            {
                // For development storage, return a default name
                return "devstoreaccount1";
            }
        }
    }

    public Task<Response<BlobServiceProperties>> GetPropertiesAsync(CancellationToken cancellationToken = default)
    {
        return _client.GetPropertiesAsync(cancellationToken);
    }
}

/// <summary>
/// Health check implementation for Azure Blob Storage
/// </summary>
public class BlobStorageHealthCheck : EventStore.Domain.Health.IHealthCheck, Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
{
    private readonly ILogger<BlobStorageHealthCheck> _logger;
    private readonly IBlobServiceClient _client;
    private readonly BlobStorageHealthCheckOptions _options;

    public string ComponentName => "BlobStorage";

    public BlobStorageHealthCheck(
        ILogger<BlobStorageHealthCheck> logger,
        IBlobServiceClient client,
        IOptions<BlobStorageHealthCheckOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<EventStore.Domain.Health.HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var result = await CheckHealthInternalAsync(cancellationToken);
        return new EventStore.Domain.Health.HealthCheckResult(
            ComponentName,
            result.Status switch
            {
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy => EventStore.Domain.Health.HealthStatus.Healthy,
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded => EventStore.Domain.Health.HealthStatus.Degraded,
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy => EventStore.Domain.Health.HealthStatus.Unhealthy,
                _ => EventStore.Domain.Health.HealthStatus.Unhealthy
            },
            result.Description ?? "Unknown status",
            result.Data?.ToDictionary(x => x.Key, x => x.Value));
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
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromMilliseconds(_options.TimeoutMs));

            var properties = await _client.GetPropertiesAsync(cts.Token);
            var data = new Dictionary<string, object>();

            if (properties?.Value == null)
            {
                return new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult(
                    Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
                    "Unable to retrieve blob storage properties",
                    data: data);
            }

            if (_options.IncludeDetailedInfo)
            {
                data = new Dictionary<string, object>
                {
                    { "accountName", _client.AccountName },
                    { "defaultServiceVersion", properties.Value.DefaultServiceVersion ?? "unknown" }
                };
            }

            return new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult(
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy,
                "Blob storage is accessible and responding",
                data: data);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Blob storage health check timed out after {TimeoutMs}ms", _options.TimeoutMs);
            return new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult(
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
                "Unable to retrieve blob storage properties",
                data: new Dictionary<string, object>
                {
                    { "error", "Timeout" },
                    { "timeoutMs", _options.TimeoutMs }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking blob storage health");
            return new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult(
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
                "Error accessing blob storage",
                ex,
                _options.IncludeDetailedInfo ? new Dictionary<string, object>
                {
                    { "AccountName", _client.AccountName },
                    { "Error", ex.Message }
                } : null);
        }
    }
}