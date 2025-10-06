using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Http;
using System.Net;
using TodoApp.Application.DTOs;
using TodoApp.Application.Interfaces;
using TodoApp.Functions.Helpers;

namespace TodoApp.Functions.Functions;

public class ImportFunctions
{
    private readonly ILogger<ImportFunctions> _logger;
    private readonly ICsvImportService _csvImportService;
    private readonly IServiceBusService _serviceBusService;
    private readonly IBlobService _blobService;
    private readonly IJwtService _jwtService;

    public ImportFunctions(
        ILogger<ImportFunctions> logger,
        ICsvImportService csvImportService,
        IServiceBusService serviceBusService,
        IBlobService blobService,
        IJwtService jwtService)
    {
        _logger = logger;
        _csvImportService = csvImportService;
        _serviceBusService = serviceBusService;
        _blobService = blobService;
        _jwtService = jwtService;
    }

    [Function("ImportTodos")]
    [OpenApiOperation(operationId: "ImportTodos", tags: new[] { "Import" }, Summary = "Import todos from CSV file (requires JWT token)", Description = "Upload a CSV file to import multiple todo items. The CSV should have columns: Title, Description, DueDate, Priority, IsCompleted")]
    [OpenApiSecurity("Bearer", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT")]
    [OpenApiRequestBody(
        contentType: "multipart/form-data", 
        bodyType: typeof(FileUploadModel), 
        Required = true, 
        Description = "Select a CSV file to upload")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Accepted, contentType: "application/json", bodyType: typeof(object), Summary = "Import request accepted and queued for processing")]
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

            // Read CSV file from multipart form data
            var (csvContent, fileName) = await ReadMultipartFormDataAsync(req);
            
            if (csvContent == null || csvContent.Length == 0)
            {
                _logger.LogWarning("Empty CSV file received for user {UserId}", userId);
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteAsJsonAsync(new { error = "CSV file is required" });
                return badRequest;
            }

            _logger.LogInformation("Uploading CSV file to blob storage for user {UserId}, file size: {Size}", 
                userId, csvContent.Length);

            // Upload CSV file to blob storage
            var blobUrl = await _blobService.UploadCsvAsync(csvContent, fileName);

            // Create import message
            var importMessage = new ImportMessage
            {
                UserId = userId.ToString(),
                BlobUrl = blobUrl,
                FileName = fileName,
                RequestId = Guid.NewGuid().ToString(),
                RequestedAt = DateTime.UtcNow
            };

            // Send message to ServiceBus queue for async processing
            var requestId = await _serviceBusService.SendImportMessageAsync(importMessage);

            // Return accepted response with request ID for tracking
            var response = req.CreateResponse(HttpStatusCode.Accepted);
            await response.WriteAsJsonAsync(new 
            { 
                message = "Import request accepted and queued for processing",
                requestId = requestId,
                status = "Queued"
            });

            _logger.LogInformation("CSV import request queued for user {UserId}. RequestId: {RequestId}", 
                userId, requestId);

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

    private async Task<(byte[] content, string fileName)> ReadMultipartFormDataAsync(HttpRequestData req)
    {
        try
        {
            var contentType = req.Headers.TryGetValues("Content-Type", out var contentTypeValues) ? 
                contentTypeValues.FirstOrDefault() : null;
            
            if (string.IsNullOrEmpty(contentType) || !contentType.Contains("multipart/form-data"))
            {
                // Fallback: treat entire body as CSV content for testing
                using var memoryStream = new MemoryStream();
                await req.Body.CopyToAsync(memoryStream);
                var content = memoryStream.ToArray();
                return (content, "upload.csv");
            }

            // Extract boundary from content type
            var boundary = ExtractBoundary(contentType);
            if (string.IsNullOrEmpty(boundary))
            {
                throw new InvalidOperationException("Boundary not found in multipart data");
            }

            using var reader = new StreamReader(req.Body);
            var body = await reader.ReadToEndAsync();
            
            // Parse multipart data
            var parts = ParseMultipartData(body, boundary);
            
            // Find file part
            var filePart = parts.FirstOrDefault(p => !string.IsNullOrEmpty(p.FileName));
            if (filePart == null)
            {
                throw new InvalidOperationException("No file found in multipart data");
            }

            // Validate CSV file extension
            if (string.IsNullOrEmpty(filePart.FileName) || !filePart.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Only CSV files are allowed");
            }

            return (System.Text.Encoding.UTF8.GetBytes(filePart.Content), filePart.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading multipart form data");
            throw;
        }
    }

    private string? ExtractBoundary(string contentType)
    {
        var boundaryIndex = contentType.IndexOf("boundary=");
        if (boundaryIndex == -1) return null;
        
        var boundary = contentType.Substring(boundaryIndex + 9);
        return boundary.Trim('"');
    }

    private List<MultipartSection> ParseMultipartData(string body, string boundary)
    {
        var parts = new List<MultipartSection>();
        var sections = body.Split(new[] { "--" + boundary }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var section in sections)
        {
            if (string.IsNullOrWhiteSpace(section) || section.StartsWith("--")) continue;
            
            var headerEndIndex = section.IndexOf("\r\n\r\n");
            if (headerEndIndex == -1) continue;
            
            var headerSection = section.Substring(0, headerEndIndex);
            var contentSection = section.Substring(headerEndIndex + 4).TrimEnd('\r', '\n');
            
            var fileName = ExtractFileName(headerSection);
            var fieldName = ExtractFieldName(headerSection);
            
            parts.Add(new MultipartSection
            {
                FieldName = fieldName,
                FileName = fileName,
                Content = contentSection
            });
        }
        
        return parts;
    }

    private string? ExtractFileName(string header)
    {
        var fileNameMatch = System.Text.RegularExpressions.Regex.Match(header, @"filename=""([^""]+)""");
        return fileNameMatch.Success ? fileNameMatch.Groups[1].Value : null;
    }

    private string? ExtractFieldName(string header)
    {
        var nameMatch = System.Text.RegularExpressions.Regex.Match(header, @"name=""([^""]+)""");
        return nameMatch.Success ? nameMatch.Groups[1].Value : null;
    }
}

public class MultipartSection
{
    public string? FieldName { get; set; }
    public string? FileName { get; set; }
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Model for file upload in multipart/form-data format
/// </summary>
public class FileUploadModel
{
    /// <summary>
    /// CSV file to upload - select a .csv file from your computer
    /// </summary>
    [System.ComponentModel.DataAnnotations.Display(Name = "file")]
    [Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes.OpenApiProperty(
        Description = "CSV file (binary data)")]
    [System.ComponentModel.DataAnnotations.Required]
    public byte[] file { get; set; } = Array.Empty<byte>();
}

