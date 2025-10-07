using TodoApp.Application.DTOs;

namespace TodoApp.Application.Interfaces.Services;

/// <summary>
/// Service for importing todos from CSV files
/// </summary>
public interface ICsvImportService
{
    /// <summary>
    /// Parse CSV content and convert to ImportTodoItem list
    /// </summary>
    /// <param name="csvContent">Raw CSV content as string</param>
    /// <returns>List of parsed todo items</returns>
    Task<List<ImportTodoItem>> ParseCsvAsync(string csvContent);

    /// <summary>
    /// Validate a single import todo item and convert to CreateTodoRequest
    /// </summary>
    /// <param name="item">Item to validate</param>
    /// <param name="rowNumber">Row number for error reporting</param>
    /// <returns>Validation result with converted request if valid</returns>
    ImportValidationResult ValidateImportItem(ImportTodoItem item, int rowNumber);

    /// <summary>
    /// Import todos for a specific user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="csvContent">CSV content to import</param>
    /// <returns>Import result summary</returns>
    Task<ImportResponse> ImportTodosAsync(long userId, string csvContent);
}