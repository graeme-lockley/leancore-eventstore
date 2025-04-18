using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EventStore.Domain.Health;

namespace EventStore.Infrastructure.Health;

/// <summary>
/// Defines the interface for interacting with Azure Blob Storage service.
/// This abstraction allows for easier testing and mocking of blob storage operations.
/// </summary>
public interface IBlobServiceClient
{
    /// <summary>
    /// Gets the name of the storage account.
    /// </summary>
    /// <value>The storage account name, or "devstoreaccount1" for local development storage.</value>
    string AccountName { get; }

    /// <summary>
    /// Gets the properties of the blob service asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A response containing the blob service properties.</returns>
    Task<Response<BlobServiceProperties>> GetPropertiesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Wraps the Azure BlobServiceClient to implement the IBlobServiceClient interface.
/// Provides additional handling for development storage scenarios.
/// </summary>
public class BlobServiceClientWrapper : IBlobServiceClient
{
    private readonly BlobServiceClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobServiceClientWrapper"/> class.
    /// </summary>
    /// <param name="client">The Azure BlobServiceClient to wrap.</param>
    /// <exception cref="ArgumentNullException">Thrown when client is null.</exception>
    public BlobServiceClientWrapper(BlobServiceClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public Task<Response<BlobServiceProperties>> GetPropertiesAsync(CancellationToken cancellationToken = default)
    {
        return _client.GetPropertiesAsync(cancellationToken);
    }
}

/// <summary>
/// Implements a health check for Azure Blob Storage.
/// Monitors the availability and performance of the blob storage service.
/// </summary>
public class BlobStorageHealthCheck : IHealthCheck
{
    private readonly IBlobServiceClient _blobServiceClient;
    private readonly ILogger<BlobStorageHealthCheck> _logger;
    private readonly BlobStorageHealthCheckOptions _options;

    /// <inheritdoc/>
    public string ComponentName => "BlobStorage";

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobStorageHealthCheck"/> class.
    /// </summary>
    /// <param name="blobServiceClient">The blob service client to use for health checks.</param>
    /// <param name="logger">The logger for recording health check activities.</param>
    /// <param name="options">Configuration options for the health check.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public BlobStorageHealthCheck(
        IBlobServiceClient blobServiceClient,
        ILogger<BlobStorageHealthCheck> logger,
        IOptions<BlobStorageHealthCheckOptions> options)
    {
        _blobServiceClient = blobServiceClient ?? throw new ArgumentNullException(nameof(blobServiceClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Performs a health check on the blob storage service.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="HealthCheckResult"/> indicating:
    /// - Healthy: When the blob storage is accessible and responding within expected timeframes
    /// - Degraded: When the operation times out
    /// - Unhealthy: When the blob storage is inaccessible or encounters errors
    /// </returns>
    /// <remarks>
    /// The health check attempts to retrieve blob service properties and measures the response time.
    /// Additional diagnostic information is included in the result based on the configuration options.
    /// </remarks>
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

            if (_options.IncludeDetailedInfo && properties?.Value != null)
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
            _logger.LogWarning("Blob storage health check timed out after {Timeout}ms: {Exception}", _options.TimeoutMs, ex.Message);
            
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
            _logger.LogError(ex, "Blob storage health check failed: {Exception}", ex.Message);
            
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