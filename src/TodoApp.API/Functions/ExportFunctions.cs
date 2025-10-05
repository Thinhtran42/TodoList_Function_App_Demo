using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System.Net;
using TodoApp.Application.DTOs;
using TodoApp.Application.Interfaces;
using TodoApp.API.Helpers;

namespace TodoApp.API.Functions;

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
        try
        {
            _logger.LogInformation("Starting CSV export for user's todos");

            var userId = AuthHelper.GetUserIdFromToken(req, _jwtService);

            // Extract query parameters for filtering
            var queryParams = QueryParameterHelper.ExtractExportQueryParameters(req);

            // Get todos based on filters
            var todos = await _todoService.GetTodosAsync(userId, queryParams);

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
            var csvData = await _csvExportService.ExportTodosToCsvAsync(todos.Items);

            // Generate unique filename
            var fileName = $"todos_export_{userId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

            // Upload to blob storage and get SAS URL
            var downloadUrl = await _csvExportService.UploadCsvToBlobAsync(csvData, fileName);

            _logger.LogInformation("CSV export completed for user {UserId}. File: {FileName}, Count: {Count}", 
                userId, fileName, todos.Items.Count());

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new ExportResponse
            {
                Message = "Export completed successfully",
                DownloadUrl = downloadUrl,
                ExportedCount = todos.Items.Count(),
                ExportedAt = DateTime.UtcNow,
                FileName = fileName
            });

            return response;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized access during export: {Message}", ex.Message);
            var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
            await unauthorizedResponse.WriteAsJsonAsync(new { error = ex.Message });
            return unauthorizedResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during CSV export");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = "Export failed. Please try again later." });
            return errorResponse;
        }
    }


}