using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using CsvHelper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;
using TodoApp.Application.DTOs;
using TodoApp.Application.Interfaces;
using TodoApp.Infrastructure.Data.Models;

namespace TodoApp.Infrastructure.Services;

public class CsvExportService : ICsvExportService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<CsvExportService> _logger;
    private const string ContainerName = "todo-exports";

    public CsvExportService(IConfiguration configuration, ILogger<CsvExportService> logger)
    {
        var connectionString = configuration.GetConnectionString("AzureStorage");
        _blobServiceClient = new BlobServiceClient(connectionString);
        _logger = logger;
    }

    public async Task<byte[]> ExportTodosToCsvAsync(IEnumerable<TodoDto> todos)
    {
        try
        {
            var todoList = todos.ToList();
            _logger.LogInformation("Starting CSV export for {Count} todos", todoList.Count);

            using var memoryStream = new MemoryStream();
            using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

            // Map TodoDto to CSV-friendly format
            var csvRecords = todoList.Select(todo => new TodoCsvRecord
            {
                Id = todo.Id.ToString(),
                Title = todo.Title,
                Description = todo.Description ?? string.Empty,
                IsCompleted = todo.IsCompleted,
                Priority = todo.Priority.ToString(),
                Category = todo.Category.ToString(),
                DueDate = todo.DueDate?.ToString("yyyy-MM-dd HH:mm:ss"),
                Tags = todo.Tags ?? string.Empty,
                CreatedAt = todo.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                UpdatedAt = todo.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss")
            }).ToList();

            _logger.LogDebug("CSV records mapped successfully. Writing to stream...");

            await csv.WriteRecordsAsync(csvRecords);
            await writer.FlushAsync();

            var csvSize = memoryStream.ToArray().Length;
            _logger.LogInformation("CSV export completed successfully. Size: {Size} bytes, Records: {Count}",
                csvSize, csvRecords.Count);

            return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting todos to CSV");
            throw;
        }
    }

    public async Task<string> UploadCsvToBlobAsync(byte[] csvData, string fileName)
    {
        try
        {
            _logger.LogInformation("Starting CSV upload to blob storage. File: {FileName}, Size: {Size} bytes",
                fileName, csvData.Length);

            // Ensure container exists
            var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
            var containerCreated = await containerClient.CreateIfNotExistsAsync();

            if (containerCreated != null)
            {
                _logger.LogInformation("Created new blob container: {ContainerName}", ContainerName);
            }

            // Clean up old files (optional - files older than 7 days)
            _logger.LogDebug("Starting cleanup of old CSV files");
            await CleanupOldFilesAsync(containerClient);

            // Upload blob
            var blobClient = containerClient.GetBlobClient(fileName);
            using var stream = new MemoryStream(csvData);

            _logger.LogDebug("Uploading CSV file to blob storage: {BlobName}", fileName);
            await blobClient.UploadAsync(stream, overwrite: true);
            _logger.LogInformation("CSV file uploaded successfully: {BlobName}", fileName);

            // Generate SAS URL (valid for 1 hour)
            if (blobClient.CanGenerateSasUri)
            {
                var sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = ContainerName,
                    BlobName = fileName,
                    ExpiresOn = DateTimeOffset.UtcNow.AddHours(1),
                    Resource = "b"
                };

                sasBuilder.SetPermissions(BlobSasPermissions.Read);

                var sasUri = blobClient.GenerateSasUri(sasBuilder);
                _logger.LogInformation("Generated SAS URI for blob: {BlobName}, Expires: {ExpiresOn}",
                    fileName, sasBuilder.ExpiresOn);
                return sasUri.ToString();
            }
            else
            {
                _logger.LogWarning("Cannot generate SAS URI for blob: {BlobName}. Returning public URI", fileName);
                return blobClient.Uri.ToString();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading CSV to blob storage. File: {FileName}", fileName);
            throw;
        }
    }

    /// <summary>
    /// Clean up old CSV files (older than 7 days) to prevent storage cost accumulation
    /// </summary>
    private async Task CleanupOldFilesAsync(BlobContainerClient containerClient)
    {
        try
        {
            var cutoffDate = DateTimeOffset.UtcNow.AddDays(-7);
            _logger.LogDebug("Cleaning up CSV files older than {CutoffDate}", cutoffDate);

            var deletedCount = 0;
            await foreach (var blobItem in containerClient.GetBlobsAsync(prefix: "csv-exports/"))
            {
                if (blobItem.Properties.LastModified < cutoffDate)
                {
                    var blobClient = containerClient.GetBlobClient(blobItem.Name);
                    var deleted = await blobClient.DeleteIfExistsAsync();

                    if (deleted)
                    {
                        deletedCount++;
                        _logger.LogInformation("Deleted old CSV file: {FileName}, LastModified: {LastModified}",
                            blobItem.Name, blobItem.Properties.LastModified);
                    }
                }
            }

            if (deletedCount > 0)
            {
                _logger.LogInformation("Cleanup completed. Deleted {Count} old CSV files", deletedCount);
            }
            else
            {
                _logger.LogDebug("No old CSV files to cleanup");
            }
        }
        catch (Exception ex)
        {
            // Log error but don't fail the upload process
            _logger.LogWarning(ex, "Failed to cleanup old CSV files");
        }
    }
}

public class TodoCsvRecord
{
    public string Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public string Priority { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? DueDate { get; set; }
    public string Tags { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
}