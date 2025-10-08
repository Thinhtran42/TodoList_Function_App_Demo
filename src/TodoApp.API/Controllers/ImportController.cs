using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoApp.API.Helpers;
using TodoApp.Application.DTOs;
using TodoApp.Application.Interfaces.Services.DataProcessing;
using TodoApp.Application.Interfaces.Services.MessageBroker;
using TodoApp.Application.Interfaces.Services.Storage;

namespace TodoApp.API.Controllers;

[Authorize]
[ApiController]
[Route("api/todos/[controller]")]
public class ImportController : ControllerBase
{
    private readonly ILogger<ImportController> _logger;
    private readonly ICsvImportService _csvImportService;
    private readonly IServiceBusService _serviceBusService;
    private readonly IFileStorageService _blobService;

    public ImportController(
        ILogger<ImportController> logger,
        ICsvImportService csvImportService,
        IServiceBusService serviceBusService,
        IFileStorageService blobService)
    {
        _logger = logger;
        _csvImportService = csvImportService;
        _serviceBusService = serviceBusService;
        _blobService = blobService;
    }

    /// <summary>
    /// Import todos from CSV file (Async processing via Service Bus)
    /// </summary>
    /// <remarks>
    /// Upload a CSV file to import multiple todo items asynchronously.
    /// The CSV should have columns: Title, Description, DueDate, Priority, IsCompleted, Category, Tags
    /// </remarks>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [DisableRequestSizeLimit]
    [ProducesResponseType(typeof(object), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ImportTodos(IFormFile file)
    {
        try
        {
            _logger.LogInformation("Starting CSV import for user's todos");

            // Use AuthHelper to get user ID
            var userId = AuthHelper.GetUserIdOrThrow(HttpContext);

            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("Empty CSV file received for user {UserId}", userId);
                return BadRequest(new { error = "CSV file is required" });
            }

            // Read CSV content
            byte[] csvContent;
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                csvContent = memoryStream.ToArray();
            }

            _logger.LogInformation("Uploading CSV file to blob storage for user {UserId}, file size: {Size}",
                userId, csvContent.Length);

            // Upload CSV file to storage (Azure Blob or MinIO depending on configuration)
            var blobUrl = await _blobService.UploadFileAsync(csvContent, file.FileName);

            // Create import message
            var importMessage = new ImportMessage
            {
                UserId = userId.ToString(),
                BlobUrl = blobUrl,
                FileName = file.FileName,
                RequestId = Guid.NewGuid().ToString(),
                RequestedAt = DateTime.UtcNow
            };

            // Send message to ServiceBus queue for async processing
            var requestId = await _serviceBusService.SendImportMessageAsync(importMessage);

            _logger.LogInformation("CSV import request queued for user {UserId}. RequestId: {RequestId}",
                userId, requestId);

            // Return accepted response with request ID for tracking
            return Accepted(new
            {
                message = "Import request accepted and queued for processing",
                requestId = requestId,
                status = "Queued"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during CSV import");
            return StatusCode(500, new { error = "Import failed. Please try again later." });
        }
    }

    /// <summary>
    /// Import todos from CSV file (Synchronous processing)
    /// </summary>
    /// <remarks>
    /// Upload a CSV file to import multiple todo items synchronously.
    /// Returns immediate results. Use for small files only.
    /// </remarks>
    [HttpPost("sync")]
    [Consumes("multipart/form-data")]
    [DisableRequestSizeLimit]
    [ProducesResponseType(typeof(ImportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ImportTodosSync(IFormFile file)
    {
        try
        {
            _logger.LogInformation("Starting synchronous CSV import");

            // Use AuthHelper to get user ID
            var userId = AuthHelper.GetUserIdOrThrow(HttpContext);

            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "CSV file is required" });
            }

            // Read CSV content
            using var reader = new StreamReader(file.OpenReadStream());
            var csvContent = await reader.ReadToEndAsync();

            _logger.LogInformation("Processing CSV import synchronously for user {UserId}", userId);

            // Process CSV import
            var result = await _csvImportService.ImportTodosAsync(userId, csvContent);

            _logger.LogInformation("CSV import completed for user {UserId}. Status: {Status}, Imported: {Imported}, Failed: {Failed}",
                userId, result.Status, result.ImportedRecords, result.FailedRecords);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during synchronous CSV import");
            return StatusCode(500, new { error = "Import failed. Please try again later." });
        }
    }

    /// <summary>
    /// Get import template CSV file
    /// </summary>
    [HttpGet("template")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public IActionResult GetTemplate()
    {
        var csvTemplate = "Title,Description,DueDate,Priority,IsCompleted,Category,Tags\n" +
                         "Sample Todo,Sample description,2025-12-31,Medium,false,Work,work;important\n" +
                         "Another Task,Another description,2025-11-15,High,false,Personal,personal;urgent";

        var bytes = System.Text.Encoding.UTF8.GetBytes(csvTemplate);
        return File(bytes, "text/csv", "todo_import_template.csv");
    }
}
