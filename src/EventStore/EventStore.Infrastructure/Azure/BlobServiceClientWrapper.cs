using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace EventStore.Infrastructure.Azure;

public class BlobServiceClientWrapper(BlobServiceClient client) : IBlobServiceClient
{
    private readonly BlobServiceClient _client = client ?? throw new ArgumentNullException(nameof(client));

    public BlobContainerClient GetBlobContainerClient(string containerName)
    {
        return _client.GetBlobContainerClient(containerName);
    }

    public async Task<BlobServiceProperties> GetPropertiesAsync(CancellationToken cancellationToken = default)
    {
        var response = await _client.GetPropertiesAsync(cancellationToken);
        return response.Value;
    }
} 