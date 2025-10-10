using Microsoft.Extensions.Logging;
using System.Globalization;
using TodoApp.Application.DTOs;
using TodoApp.Domain.Entities;
using TodoApp.Domain.Enums;
using TodoApp.Application.Interfaces.Services.Business;
using TodoApp.Application.Interfaces.Services.DataProcessing;

namespace TodoApp.Application.Services;

public class CsvImportService : ICsvImportService
{
    private readonly ILogger<CsvImportService> _logger;
    private readonly ITodoService _todoService;

    public CsvImportService(
        ILogger<CsvImportService> logger,
        ITodoService todoService)
    {
        _logger = logger;
        _todoService = todoService;
    }

    public Task<List<ImportTodoItem>> ParseCsvAsync(string csvContent)
    {
        var items = new List<ImportTodoItem>();
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length == 0)
        {
            return Task.FromResult(items);
        }

        // Skip header row if present
        var startIndex = IsHeaderRow(lines[0]) ? 1 : 0;

        for (int i = startIndex; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            try
            {
                var item = ParseCsvLine(line);
                if (item != null)
                {
                    items.Add(item);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to parse CSV line {LineNumber}: {Line}. Error: {Error}",
                    i + 1, line, ex.Message);
            }
        }

        return Task.FromResult(items);
    }

    public ImportValidationResult ValidateImportItem(ImportTodoItem item, int rowNumber)
    {
        var result = new ImportValidationResult();
        var errors = new List<string>();

        // Validate Title (required)
        if (string.IsNullOrWhiteSpace(item.Title))
        {
            errors.Add($"Row {rowNumber}: Title is required");
        }

        // Validate Priority
        if (!Enum.TryParse<Priority>(item.Priority, true, out var priority))
        {
            errors.Add($"Row {rowNumber}: Invalid Priority '{item.Priority}'. Valid values: Low, Medium, High, Critical");
            priority = Priority.Medium; // Default fallback
        }

        // Validate Category
        if (!Enum.TryParse<Category>(item.Category, true, out var category))
        {
            errors.Add($"Row {rowNumber}: Invalid Category '{item.Category}'. Valid values: Personal, Work, Shopping, Health, General");
            category = Category.General; // Default fallback
        }

        // Validate DueDate
        DateTime? dueDate = null;
        if (!string.IsNullOrWhiteSpace(item.DueDate))
        {
            if (!DateTime.TryParse(item.DueDate, out var parsedDate))
            {
                errors.Add($"Row {rowNumber}: Invalid DueDate format '{item.DueDate}'. Use format: yyyy-MM-dd or MM/dd/yyyy");
            }
            else
            {
                // Ensure DateTime is in UTC for PostgreSQL compatibility
                dueDate = parsedDate.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc)
                    : parsedDate.ToUniversalTime();
            }
        }

        result.IsValid = errors.Count == 0;
        result.Errors = errors;

        if (result.IsValid || errors.Count <= 2) // Allow minor errors with defaults
        {
            result.TodoRequest = new CreateTodoRequest
            {
                Title = item.Title?.Trim() ?? string.Empty,
                Description = item.Description?.Trim(),
                Priority = priority,
                Category = category,
                DueDate = dueDate,
                Tags = item.Tags?.Trim()
            };
        }

        return result;
    }

    public async Task<ImportResponse> ImportTodosAsync(long userId, string csvContent)
    {
        var response = new ImportResponse
        {
            ImportedAt = DateTime.UtcNow,
            Status = "Processing"
        };

        try
        {
            _logger.LogInformation("Starting CSV import for user {UserId}", userId);

            // Parse CSV
            var importItems = await ParseCsvAsync(csvContent);
            response.TotalRecords = importItems.Count;

            if (importItems.Count == 0)
            {
                response.Status = "Failed";
                response.Message = "No valid data found in CSV file";
                response.Errors.Add("CSV file is empty or contains no valid data");
                return response;
            }

            var importedCount = 0;
            var failedCount = 0;
            var allErrors = new List<string>();

            // Process each item
            for (int i = 0; i < importItems.Count; i++)
            {
                var item = importItems[i];
                var validation = ValidateImportItem(item, i + 2); // +2 for header and 0-based index

                if (validation.IsValid && validation.TodoRequest != null)
                {
                    try
                    {
                        await _todoService.CreateTodoAsync(userId, validation.TodoRequest);
                        importedCount++;
                        _logger.LogDebug("Successfully imported todo: {Title}", validation.TodoRequest.Title);
                    }
                    catch (Exception ex)
                    {
                        failedCount++;
                        var error = $"Row {i + 2}: Failed to create todo '{item.Title}' - {ex.Message}";
                        allErrors.Add(error);
                        _logger.LogWarning(ex, "Failed to import todo at row {Row}", i + 2);
                    }
                }
                else
                {
                    failedCount++;
                    allErrors.AddRange(validation.Errors);
                }
            }

            response.ImportedRecords = importedCount;
            response.FailedRecords = failedCount;
            response.Errors = allErrors;

            if (importedCount > 0)
            {
                response.Status = failedCount == 0 ? "Completed" : "Partial";
                response.Message = failedCount == 0
                    ? $"Successfully imported {importedCount} todos"
                    : $"Imported {importedCount} todos, {failedCount} failed";
            }
            else
            {
                response.Status = "Failed";
                response.Message = "No todos were imported";
            }

            _logger.LogInformation("CSV import completed for user {UserId}. Imported: {Imported}, Failed: {Failed}",
                userId, importedCount, failedCount);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during CSV import for user {UserId}", userId);
            response.Status = "Failed";
            response.Message = "An error occurred during import";
            response.Errors.Add($"System error: {ex.Message}");
            return response;
        }
    }

    private bool IsHeaderRow(string line)
    {
        // Check if first line looks like a header
        var lowerLine = line.ToLower();
        return lowerLine.Contains("title") || lowerLine.Contains("description") ||
               lowerLine.Contains("priority") || lowerLine.Contains("category");
    }

    private ImportTodoItem? ParseCsvLine(string line)
    {
        // Simple CSV parsing (handles basic comma separation)
        // For production, consider using a proper CSV library like CsvHelper
        var fields = SplitCsvLine(line);

        if (fields.Length < 1) return null;

        var item = new ImportTodoItem();

        // Map fields based on expected CSV format:
        // Title, Description, Priority, Category, DueDate, Tags, IsCompleted
        if (fields.Length > 0) item.Title = fields[0];
        if (fields.Length > 1) item.Description = fields[1];
        if (fields.Length > 2) item.Priority = fields[2];
        if (fields.Length > 3) item.Category = fields[3];
        if (fields.Length > 4) item.DueDate = fields[4];
        if (fields.Length > 5) item.Tags = fields[5];
        if (fields.Length > 6)
        {
            bool completed;
            bool.TryParse(fields[6], out completed);
            item.IsCompleted = completed;
        }

        return item;
    }

    private string[] SplitCsvLine(string line)
    {
        var fields = new List<string>();
        var currentField = string.Empty;
        var inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (c == '"' && (i == 0 || line[i - 1] != '\\'))
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(currentField.Trim().Trim('"'));
                currentField = string.Empty;
            }
            else
            {
                currentField += c;
            }
        }

        fields.Add(currentField.Trim().Trim('"'));
        return fields.ToArray();
    }
}