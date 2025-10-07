using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System.Net;
using TodoApp.Application.DTOs;
using TodoApp.Application.Interfaces.Services;
using TodoApp.Functions.Helpers;

namespace TodoApp.Functions.Functions;

public class ExportFunctions
{
    private readonly ILogger<ExportFunctions> _logger;
    private readonly ITodoService _todoService;
    private readonly ICsvExportService _csvExportService;
    private readonly IJwtService _jwtService;

    public ExportFunctions(
        ILogger<ExportFunctions> logger,
        ITodoService todoService,
        ICsvExportService csvExportService,
        IJwtService jwtService)
    {
        _logger = logger;
        _todoService = todoService;
        _csvExportService = csvExportService;
        _jwtService = jwtService;
    }



    [Function("ExportTodos")]
    [OpenApiOperation(operationId: "ExportTodos", tags: new[] { "Export" }, Summary = "Export user's todos to CSV file (requires JWT token)")]
    [OpenApiSecurity("Bearer", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT")]
    [OpenApiParameter(name: "isCompleted", In = ParameterLocation.Query, Type = typeof(bool?), Summary = "Filter by completion status")]
    [OpenApiParameter(name: "priority", In = ParameterLocation.Query, Type = typeof(int?), Summary = "Filter by priority (1=Low, 2=Medium, 3=High, 4=Critical)")]
    [OpenApiParameter(name: "category", In = ParameterLocation.Query, Type = typeof(int?), Summary = "Filter by category")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(ExportResponse), Summary = "CSV export successful")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/json", bodyType: typeof(object), Summary = "Unauthorized")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "application/json", bodyType: typeof(object), Summary = "Export failed")]
    public async Task<HttpResponseData> ExportTodos(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "todos/export")] HttpRequestData req)
    {
        var startTime = DateTime.UtcNow;
        long? userId = null;

        try
        {
            _logger.LogInformation("=== CSV Export Request Started ===");

            userId = AuthHelper.GetUserIdFromToken(req, _jwtService);
            _logger.LogInformation("User authenticated successfully. UserId: {UserId}", userId);

            // Extract query parameters for filtering
            var queryParams = QueryParameterHelper.ExtractExportQueryParameters(req);
            _logger.LogInformation("Export filters - IsCompleted: {IsCompleted}, Priority: {Priority}, Category: {Category}",
                queryParams.IsCompleted?.ToString() ?? "All",
                queryParams.Priority?.ToString() ?? "All",
                queryParams.Category?.ToString() ?? "All");

            // Get todos based on filters
            _logger.LogDebug("Fetching todos for user {UserId} with applied filters", userId);
            var todos = await _todoService.GetTodosAsync(userId.Value, queryParams);
            _logger.LogInformation("Retrieved {Count} todos for export", todos.Items.Count());

            if (!todos.Items.Any())
            {
                _logger.LogInformation("No todos found for export for user {UserId}", userId);
                var noDataResponse = req.CreateResponse(HttpStatusCode.OK);
                await noDataResponse.WriteAsJsonAsync(new ExportResponse
                {
                    Message = "No todos found to export",
                    DownloadUrl = null,
                    ExportedCount = 0,
                    ExportedAt = DateTime.UtcNow
                });
                return noDataResponse;
            }

            // Export to CSV
            _logger.LogDebug("Converting todos to CSV format");
            var csvData = await _csvExportService.ExportTodosToCsvAsync(todos.Items);
            _logger.LogInformation("CSV data generated. Size: {Size} bytes", csvData.Length);

            // Generate unique filename
            var fileName = $"todos_export_{userId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
            _logger.LogDebug("Generated filename: {FileName}", fileName);

            // Upload to blob storage and get SAS URL with metadata
            _logger.LogDebug("Uploading CSV to blob storage");
            var uploadResult = await _csvExportService.UploadCsvToBlobAsync(csvData, fileName);

            var duration = (DateTime.UtcNow - startTime).TotalSeconds;
            var validForMinutes = (int)(uploadResult.ExpiresAt - DateTime.UtcNow).TotalMinutes;

            _logger.LogInformation("=== CSV Export Completed Successfully === UserId: {UserId}, File: {FileName}, Count: {Count}, Duration: {Duration}s, Expires: {ExpiresAt}",
                userId, fileName, todos.Items.Count(), duration.ToString("F2"), uploadResult.ExpiresAt);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new ExportResponse
            {
                Message = "Export completed successfully",
                DownloadUrl = uploadResult.DownloadUrl,
                ExportedCount = todos.Items.Count(),
                ExportedAt = DateTime.UtcNow,
                FileName = uploadResult.FileName,
                ExpiresAt = uploadResult.ExpiresAt,
                AccessPermissions = uploadResult.Permissions,
                ValidForMinutes = validForMinutes,
                StorageInfo = $"Stored in Azure Blob Storage ({uploadResult.ContainerName}), Size: {uploadResult.FileSizeBytes} bytes"
            });

            return response;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("=== Export Failed - Unauthorized === UserId: {UserId}, Message: {Message}",
                userId?.ToString() ?? "Unknown", ex.Message);
            var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
            await unauthorizedResponse.WriteAsJsonAsync(new { error = ex.Message });
            return unauthorizedResponse;
        }
        catch (Exception ex)
        {
            var duration = (DateTime.UtcNow - startTime).TotalSeconds;
            _logger.LogError(ex, "=== Export Failed - Error === UserId: {UserId}, Duration: {Duration}s",
                userId?.ToString() ?? "Unknown", duration.ToString("F2"));
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = "Export failed. Please try again later." });
            return errorResponse;
        }
    }


}