using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace EventStore.Infrastructure.Azure;

public interface IBlobServiceClient
{
    BlobContainerClient GetBlobContainerClient(string containerName);
    Task<BlobServiceProperties> GetPropertiesAsync(CancellationToken cancellationToken = default);
} 