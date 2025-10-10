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
    private readonly IMessageQueueService _messageQueueService;
    private readonly IFileStorageService _storageService;

    public ImportController(
        ILogger<ImportController> logger,
        ICsvImportService csvImportService,
        IMessageQueueService messageQueueService,
        IFileStorageService storageService)
    {
        _logger = logger;
        _csvImportService = csvImportService;
        _messageQueueService = messageQueueService;
        _storageService = storageService;
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

            _logger.LogInformation("Uploading CSV file to storage for user {UserId}, file size: {Size}",
                userId, csvContent.Length);

            // Upload CSV file to storage (Azure Blob or MinIO depending on configuration)
            var fileUrl = await _storageService.UploadFileAsync(csvContent, file.FileName);

            // Create import message
            var importMessage = new ImportMessage
            {
                UserId = userId.ToString(),
                FileUrl = fileUrl,
                FileName = file.FileName,
                RequestId = Guid.NewGuid().ToString(),
                RequestedAt = DateTime.UtcNow
            };

            // Send message to message queue for async processing
            var requestId = await _messageQueueService.SendMessageAsync(importMessage);

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
}
