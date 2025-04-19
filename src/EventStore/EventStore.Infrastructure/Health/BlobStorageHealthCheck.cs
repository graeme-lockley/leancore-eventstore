using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using HealthCheckResult = EventStore.Domain.Health.HealthCheckResult;
using IHealthCheck = EventStore.Domain.Health.IHealthCheck;

namespace EventStore.Infrastructure.Health;

public interface IBlobServiceClient
{
    string AccountName { get; }
    Task<Response<BlobServiceProperties>> GetPropertiesAsync(CancellationToken cancellationToken = default);
}

public class BlobServiceClientWrapper(BlobServiceClient client) : IBlobServiceClient
{
    private readonly BlobServiceClient _client = client ?? throw new ArgumentNullException(nameof(client));

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
public class BlobStorageHealthCheck(
    ILogger<BlobStorageHealthCheck> logger,
    IBlobServiceClient client,
    IOptions<BlobStorageHealthCheckOptions> options)
    : IHealthCheck, Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
{
    private readonly ILogger<BlobStorageHealthCheck> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IBlobServiceClient _client = client ?? throw new ArgumentNullException(nameof(client));
    private readonly BlobStorageHealthCheckOptions _options = options.Value ?? throw new ArgumentNullException(nameof(options));

    public string ComponentName => "BlobStorage";

    public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var result = await CheckHealthInternalAsync(cancellationToken);
        return new HealthCheckResult(
            ComponentName,
            result.Status switch
            {
                HealthStatus.Healthy => Domain.Health.HealthStatus.Healthy,
                HealthStatus.Degraded => Domain.Health.HealthStatus.Degraded,
                _ => Domain.Health.HealthStatus.Unhealthy
            },
            result.Description ?? "Unknown status",
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
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromMilliseconds(_options.TimeoutMs));

            var properties = await _client.GetPropertiesAsync(cts.Token);
            var data = new Dictionary<string, object>();

            if (_options.IncludeDetailedInfo)
            {
                data = new Dictionary<string, object>
                {
                    { "accountName", _client.AccountName },
                    { "defaultServiceVersion", properties.Value.DefaultServiceVersion ?? "unknown" }
                };
            }

            return new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult(
                HealthStatus.Healthy,
                "Blob storage is accessible and responding",
                data: data);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Blob storage health check timed out after {TimeoutMs}ms", _options.TimeoutMs);
            return new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult(
                HealthStatus.Degraded,
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
                HealthStatus.Unhealthy,
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