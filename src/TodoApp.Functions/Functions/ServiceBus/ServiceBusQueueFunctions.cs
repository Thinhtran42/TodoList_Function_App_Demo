using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TodoApp.Application.DTOs;
using TodoApp.Application.Interfaces.Services;

namespace TodoApp.Functions.Functions;

public class ServiceBusQueueFunctions
{
    private readonly ILogger<ServiceBusQueueFunctions> _logger;
    private readonly ICsvImportService _csvImportService;
    private readonly IBlobService _blobService;

    public ServiceBusQueueFunctions(
        ILogger<ServiceBusQueueFunctions> logger,
        ICsvImportService csvImportService,
        IBlobService blobService)
    {
        _logger = logger;
        _csvImportService = csvImportService;
        _blobService = blobService;
    }

    [Function("ProcessImportQueue")]
    public async Task ProcessImportMessage(
        [ServiceBusTrigger("%ServiceBusQueueName%", Connection = "ConnectionStrings:ServiceBus")] string messageBody,
        FunctionContext context)
    {
        try
        {
            _logger.LogInformation("Processing import message from ServiceBus queue");

            // Deserialize message
            var importMessage = JsonSerializer.Deserialize<ImportMessage>(messageBody);
            if (importMessage == null)
            {
                _logger.LogError("Failed to deserialize import message");
                return;
            }

            _logger.LogInformation("Processing CSV import for RequestId: {RequestId}, UserId: {UserId}, FileName: {FileName}",
                importMessage.RequestId, importMessage.UserId, importMessage.FileName);

            // Download CSV content from blob storage
            _logger.LogInformation("Downloading CSV from blob storage: {BlobUrl}", importMessage.BlobUrl);
            var csvContent = await _blobService.DownloadCsvAsync(importMessage.BlobUrl);

            // Convert string userId to long
            if (!long.TryParse(importMessage.UserId, out var userId))
            {
                _logger.LogError("Invalid UserId format in import message: {UserId}", importMessage.UserId);
                throw new ArgumentException("Invalid UserId format");
            }

            // Process CSV import using the existing service
            var result = await _csvImportService.ImportTodosAsync(userId, csvContent);

            _logger.LogInformation("CSV import completed for RequestId: {RequestId}. Status: {Status}, Imported: {Imported}, Failed: {Failed}",
                importMessage.RequestId, result.Status, result.ImportedRecords, result.FailedRecords);

            // Log detailed results for monitoring
            if (result.FailedRecords > 0)
            {
                _logger.LogWarning("Import had errors for RequestId: {RequestId}. Failed records: {FailedCount}",
                    importMessage.RequestId, result.FailedRecords);

                if (result.Errors != null && result.Errors.Count > 0)
                {
                    foreach (var error in result.Errors)
                    {
                        _logger.LogWarning("Import error for RequestId: {RequestId}: {Error}",
                            importMessage.RequestId, error);
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize ServiceBus message");
            throw; // This will move the message to dead letter queue
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing import message from ServiceBus queue");
            throw; // This will move the message to dead letter queue after retries
        }
    }
}