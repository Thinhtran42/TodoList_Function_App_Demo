using Microsoft.Azure.Functions.Worker.Http;
using TodoApp.Application.Common;
using TodoApp.Application.DTOs;
using TodoApp.Domain.Entities;

namespace TodoApp.API.Helpers;

/// <summary>
/// Helper for extracting and parsing query parameters from HTTP requests
/// </summary>
public static class QueryParameterHelper
{
    /// <summary>
    /// Extract TodoQueryParameters from HTTP request query string
    /// </summary>
    public static TodoQueryParameters ExtractTodoQueryParameters(HttpRequestData req)
    {
        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);

        var parameters = new TodoQueryParameters();

        if (bool.TryParse(query["isCompleted"], out var isCompleted))
            parameters.IsCompleted = isCompleted;

        if (Enum.TryParse<Priority>(query["priority"], out var priority))
            parameters.Priority = priority;

        if (Enum.TryParse<Category>(query["category"], out var category))
            parameters.Category = category;

        if (DateTime.TryParse(query["dueDateFrom"], out var dueDateFrom))
            parameters.DueDateFrom = dueDateFrom;

        if (DateTime.TryParse(query["dueDateTo"], out var dueDateTo))
            parameters.DueDateTo = dueDateTo;

        parameters.SearchTerm = query["searchTerm"];
        parameters.Tags = query["tags"];

        // Default pagination if not specified
        if (int.TryParse(query["page"], out var page) && page > 0)
            parameters.Page = page;

        if (int.TryParse(query["pageSize"], out var pageSize) && pageSize > 0)
            parameters.PageSize = pageSize;

        // Sorting
        parameters.SortBy = query["sortBy"] ?? "CreatedAt";
        
        if (bool.TryParse(query["sortDescending"], out var sortDesc))
            parameters.SortDescending = sortDesc;

        return parameters;
    }

    /// <summary>
    /// Extract TodoQueryParameters optimized for export (no pagination)
    /// </summary>
    public static TodoQueryParameters ExtractExportQueryParameters(HttpRequestData req)
    {
        var parameters = ExtractTodoQueryParameters(req);
        
        // Override pagination for export - get all records
        parameters.Page = 1;
        parameters.PageSize = 10000; // Large page size
        
        // Default sorting for export
        parameters.SortBy = "CreatedAt";
        parameters.SortDescending = false;
        
        return parameters;
    }
}