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

public class ImportFunctions
{
    private readonly ILogger<ImportFunctions> _logger;
    private readonly ICsvImportService _csvImportService;
    private readonly IJwtService _jwtService;

    public ImportFunctions(
        ILogger<ImportFunctions> logger,
        ICsvImportService csvImportService,
        IJwtService jwtService)
    {
        _logger = logger;
        _csvImportService = csvImportService;
        _jwtService = jwtService;
    }

    [Function("ImportTodos")]
    [OpenApiOperation(operationId: "ImportTodos", tags: new[] { "Import" }, Summary = "Import todos from CSV content (requires JWT token)")]
    [OpenApiSecurity("Bearer", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT")]
    [OpenApiRequestBody(contentType: "text/plain", bodyType: typeof(string), Required = true, Description = "CSV content to import")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(ImportResponse), Summary = "Import completed")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(object), Summary = "Invalid CSV data")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/json", bodyType: typeof(object), Summary = "Unauthorized")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "application/json", bodyType: typeof(object), Summary = "Import failed")]
    public async Task<HttpResponseData> ImportTodos(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "todos/import")] HttpRequestData req)
    {
        try
        {
            _logger.LogInformation("Starting CSV import for user's todos");

            // Get user ID from JWT token
            var userId = AuthHelper.GetUserIdFromToken(req, _jwtService);

            // Read CSV content from request body
            var csvContent = await ReadRequestBodyAsync(req);
            
            if (string.IsNullOrWhiteSpace(csvContent))
            {
                _logger.LogWarning("Empty CSV content received for user {UserId}", userId);
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteAsJsonAsync(new { error = "CSV content is required" });
                return badRequest;
            }

            _logger.LogInformation("Processing CSV import for user {UserId}, content length: {Length}", 
                userId, csvContent.Length);

            // Import todos using the service
            var result = await _csvImportService.ImportTodosAsync(userId, csvContent);

            // Return appropriate response based on result
            var statusCode = result.Status switch
            {
                "Completed" => HttpStatusCode.OK,
                "Partial" => HttpStatusCode.OK, // Partial success is still OK
                "Failed" => HttpStatusCode.BadRequest,
                _ => HttpStatusCode.InternalServerError
            };

            var response = req.CreateResponse(statusCode);
            await response.WriteAsJsonAsync(result);

            _logger.LogInformation("CSV import completed for user {UserId}. Status: {Status}, Imported: {Imported}, Failed: {Failed}", 
                userId, result.Status, result.ImportedRecords, result.FailedRecords);

            return response;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized access during import: {Message}", ex.Message);
            var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
            await unauthorizedResponse.WriteAsJsonAsync(new { error = ex.Message });
            return unauthorizedResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during CSV import");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = "Import failed. Please try again later." });
            return errorResponse;
        }
    }

    private async Task<string> ReadRequestBodyAsync(HttpRequestData req)
    {
        using var reader = new StreamReader(req.Body);
        return await reader.ReadToEndAsync();
    }
}