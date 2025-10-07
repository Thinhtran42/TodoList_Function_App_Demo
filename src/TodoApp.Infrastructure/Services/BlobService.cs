using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using TodoApp.Application.Interfaces.Services;

namespace TodoApp.Infrastructure.Services;

public class BlobService : IBlobService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<BlobService> _logger;

    public BlobService(IConfiguration configuration, ILogger<BlobService> logger)
    {
        var connectionString = configuration.GetConnectionString("AzureStorage");
        _blobServiceClient = new BlobServiceClient(connectionString);
        _logger = logger;
    }

    public async Task<string> UploadCsvAsync(byte[] csvContent, string fileName, string containerName = "imports")
    {
        try
        {
            // Ensure container exists
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();

            // Create unique blob name with timestamp to avoid conflicts
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var uniqueFileName = $"{timestamp}-{fileName}";
            var blobClient = containerClient.GetBlobClient(uniqueFileName);

            // Upload blob
            using var stream = new MemoryStream(csvContent);
            await blobClient.UploadAsync(stream, overwrite: true);

            _logger.LogInformation("CSV file uploaded to blob storage: {BlobName}", uniqueFileName);

            return blobClient.Uri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading CSV to blob storage: {FileName}", fileName);
            throw;
        }
    }

    public async Task<string> DownloadCsvAsync(string blobUrl)
    {
        try
        {
            // Parse the blob URL to extract container and blob name
            var uri = new Uri(blobUrl);
            var pathSegments = uri.AbsolutePath.TrimStart('/').Split('/');
            var containerName = pathSegments[0];
            var blobName = string.Join("/", pathSegments.Skip(1));

            // Get blob client using the authenticated service client
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            // Download blob content
            var response = await blobClient.DownloadContentAsync();
            var content = response.Value.Content.ToString();

            _logger.LogInformation("CSV file downloaded from blob storage: {BlobUrl}", blobUrl);

            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading CSV from blob storage: {BlobUrl}", blobUrl);
            throw;
        }
    }
}