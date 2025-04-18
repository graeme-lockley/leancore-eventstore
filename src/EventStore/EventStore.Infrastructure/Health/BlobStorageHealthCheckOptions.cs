namespace EventStore.Infrastructure.Health;

/// <summary>
/// Configuration options for the blob storage health check
/// </summary>
public class BlobStorageHealthCheckOptions
{
    /// <summary>
    /// Gets or sets the timeout in milliseconds for the health check
    /// </summary>
    public int TimeoutMs { get; set; } = 5000; // Default 5 seconds

    /// <summary>
    /// Gets or sets whether to include detailed storage account information in the health check response
    /// </summary>
    public bool IncludeDetailedInfo { get; set; } = true;
} 