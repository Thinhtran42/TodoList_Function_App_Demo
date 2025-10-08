using CsvHelper;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;
using TodoApp.Application.DTOs;
using TodoApp.Application.Interfaces.Services.DataProcessing;
using TodoApp.Application.Interfaces.Services.Storage;
using TodoApp.Infrastructure.Data.Models;

namespace TodoApp.Infrastructure.Services.Persistence;

public class CsvExportService : ICsvExportService
{
    private readonly IFileStorageService _storageService;
    private readonly ILogger<CsvExportService> _logger;
    private const string ContainerName = "todo-exports";

    public CsvExportService(IFileStorageService storageService, ILogger<CsvExportService> logger)
    {
        _storageService = storageService;
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

    public async Task<FileUploadResult> UploadCsvToStorageAsync(byte[] csvData, string fileName)
    {
        try
        {
            _logger.LogInformation("Starting CSV upload to storage. File: {FileName}, Size: {Size} bytes",
                fileName, csvData.Length);

            // Use IFileStorageService to upload the file (works with Azure Blob, MinIO, etc.)
            var fileUrl = await _storageService.UploadFileAsync(csvData, fileName, ContainerName);

            _logger.LogInformation("CSV file uploaded successfully: {FileName}, URL: {FileUrl}", fileName, fileUrl);

            // Return result with file information
            return new FileUploadResult
            {
                DownloadUrl = fileUrl,
                FileName = fileName,
                ExpiresAt = DateTime.UtcNow.AddHours(1), // Approximate expiry for SAS URLs
                Permissions = "Read",
                ContainerName = ContainerName,
                FileSizeBytes = csvData.Length
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading CSV to storage. File: {FileName}", fileName);
            throw;
        }
    }
}

public class TodoCsvRecord
{
    public string Id { get; set; } = string.Empty;
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