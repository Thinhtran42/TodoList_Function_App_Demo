using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TodoApp.Application.Interfaces.Services.Storage;

namespace TodoApp.Infrastructure.Services.Storage;

/// <summary>
/// Azure Blob Storage implementation of IFileStorageService
/// </summary>
public class AzureBlobStorageService : IFileStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<AzureBlobStorageService> _logger;

    public AzureBlobStorageService(IConfiguration configuration, ILogger<AzureBlobStorageService> logger)
    {
        var connectionString = configuration.GetConnectionString("AzureStorage");
        _blobServiceClient = new BlobServiceClient(connectionString);
        _logger = logger;
    }

    public async Task<string> UploadFileAsync(byte[] fileContent, string fileName, string containerName = "imports")
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
            using var stream = new MemoryStream(fileContent);
            await blobClient.UploadAsync(stream, overwrite: true);

            _logger.LogInformation("File uploaded to Azure Blob Storage: {BlobName}", uniqueFileName);

            return blobClient.Uri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file to Azure Blob Storage: {FileName}", fileName);
            throw;
        }
    }

    public async Task<string> DownloadFileAsync(string fileUrl)
    {
        try
        {
            // Parse the blob URL to extract container and blob name
            var uri = new Uri(fileUrl);
            var pathSegments = uri.AbsolutePath.TrimStart('/').Split('/');
            var containerName = pathSegments[0];
            var blobName = string.Join("/", pathSegments.Skip(1));

            // Get blob client using the authenticated service client
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            // Download blob content
            var response = await blobClient.DownloadContentAsync();
            var content = response.Value.Content.ToString();

            _logger.LogInformation("File downloaded from Azure Blob Storage: {FileUrl}", fileUrl);

            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file from Azure Blob Storage: {FileUrl}", fileUrl);
            throw;
        }
    }

    public async Task<bool> DeleteFileAsync(string fileUrl)
    {
        try
        {
            var uri = new Uri(fileUrl);
            var pathSegments = uri.AbsolutePath.TrimStart('/').Split('/');
            var containerName = pathSegments[0];
            var blobName = string.Join("/", pathSegments.Skip(1));

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            var response = await blobClient.DeleteIfExistsAsync();

            _logger.LogInformation("File deleted from Azure Blob Storage: {FileUrl}, Success: {Success}",
                fileUrl, response.Value);

            return response.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file from Azure Blob Storage: {FileUrl}", fileUrl);
            return false;
        }
    }

    public async Task<bool> FileExistsAsync(string fileUrl)
    {
        try
        {
            var uri = new Uri(fileUrl);
            var pathSegments = uri.AbsolutePath.TrimStart('/').Split('/');
            var containerName = pathSegments[0];
            var blobName = string.Join("/", pathSegments.Skip(1));

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            return await blobClient.ExistsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking file existence in Azure Blob Storage: {FileUrl}", fileUrl);
            return false;
        }
    }
}