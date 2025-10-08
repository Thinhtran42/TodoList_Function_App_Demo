using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoApp.API.Helpers;
using TodoApp.Application.DTOs;
using TodoApp.Application.Interfaces.Services.Business;
using TodoApp.Application.Interfaces.Services.DataProcessing;

namespace TodoApp.API.Controllers;

[Authorize]
[ApiController]
[Route("api/todos/[controller]")]
public class ExportController : ControllerBase
{
    private readonly ILogger<ExportController> _logger;
    private readonly ITodoService _todoService;
    private readonly ICsvExportService _csvExportService;

    public ExportController(
        ILogger<ExportController> logger,
        ITodoService todoService,
        ICsvExportService csvExportService)
    {
        _logger = logger;
        _todoService = todoService;
        _csvExportService = csvExportService;
    }

    /// <summary>
    /// Export user's todos to CSV file with SAS URL (Azure Blob Storage)
    /// </summary>
    /// <remarks>
    /// Exports todos to CSV and uploads to Azure Blob Storage.
    /// Returns a time-limited SAS URL for download.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(ExportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExportTodos()
    {
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("=== CSV Export Request Started ===");

            // Use AuthHelper to get user ID
            var userId = AuthHelper.GetUserIdOrThrow(HttpContext);
            _logger.LogInformation("User authenticated successfully. UserId: {UserId}", userId);

            // Use QueryParameterHelper to extract query parameters
            var queryParams = QueryParameterHelper.ExtractExportQueryParameters(Request);

            _logger.LogInformation("Export filters - IsCompleted: {IsCompleted}, Priority: {Priority}, Category: {Category}",
                queryParams.IsCompleted?.ToString() ?? "All",
                queryParams.Priority?.ToString() ?? "All",
                queryParams.Category?.ToString() ?? "All");

            // Get todos based on filters
            _logger.LogDebug("Fetching todos for user {UserId} with applied filters", userId);
            var todos = await _todoService.GetTodosAsync(userId, queryParams);
            _logger.LogInformation("Retrieved {Count} todos for export", todos.Items.Count());

            if (!todos.Items.Any())
            {
                _logger.LogInformation("No todos found for export for user {UserId}", userId);
                return Ok(new ExportResponse
                {
                    Message = "No todos found to export",
                    DownloadUrl = null,
                    ExportedCount = 0,
                    ExportedAt = DateTime.UtcNow
                });
            }

            // Export to CSV
            _logger.LogDebug("Converting todos to CSV format");
            var csvData = await _csvExportService.ExportTodosToCsvAsync(todos.Items);
            _logger.LogInformation("CSV data generated. Size: {Size} bytes", csvData.Length);

            // Generate unique filename
            var fileName = $"todos_export_{userId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
            _logger.LogDebug("Generated filename: {FileName}", fileName);

            // Upload to storage (Azure Blob, MinIO, etc.) and get download URL with metadata
            _logger.LogDebug("Uploading CSV to storage");
            var uploadResult = await _csvExportService.UploadCsvToStorageAsync(csvData, fileName);

            var duration = (DateTime.UtcNow - startTime).TotalSeconds;
            var validForMinutes = (int)(uploadResult.ExpiresAt - DateTime.UtcNow).TotalMinutes;

            _logger.LogInformation("=== CSV Export Completed Successfully === UserId: {UserId}, File: {FileName}, Count: {Count}, Duration: {Duration}s, Expires: {ExpiresAt}",
                userId, fileName, todos.Items.Count(), duration.ToString("F2"), uploadResult.ExpiresAt);

            return Ok(new ExportResponse
            {
                Message = "Export completed successfully",
                DownloadUrl = uploadResult.DownloadUrl,
                ExportedCount = todos.Items.Count(),
                ExportedAt = DateTime.UtcNow,
                FileName = uploadResult.FileName,
                ExpiresAt = uploadResult.ExpiresAt,
                AccessPermissions = uploadResult.Permissions,
                ValidForMinutes = validForMinutes,
                StorageInfo = $"Stored in file storage ({uploadResult.ContainerName}), Size: {uploadResult.FileSizeBytes} bytes"
            });
        }
        catch (Exception ex)
        {
            var duration = (DateTime.UtcNow - startTime).TotalSeconds;
            _logger.LogError(ex, "=== Export Failed - Error === Duration: {Duration}s", duration.ToString("F2"));
            return StatusCode(500, new { error = "Export failed. Please try again later." });
        }
    }
}
