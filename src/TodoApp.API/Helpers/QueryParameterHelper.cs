using Microsoft.AspNetCore.Http;
using TodoApp.Application.Common;
using TodoApp.Application.DTOs;
using TodoApp.Domain.Enums;

namespace TodoApp.API.Helpers;

/// <summary>
/// Helper for extracting and parsing query parameters from HTTP requests
/// </summary>
public static class QueryParameterHelper
{
    /// <summary>
    /// Extract TodoQueryParameters from HTTP request query string
    /// </summary>
    public static TodoQueryParameters ExtractTodoQueryParameters(HttpRequest request)
    {
        var query = request.Query;
        var parameters = new TodoQueryParameters();

        // IsCompleted filter
        if (query.TryGetValue("isCompleted", out var isCompletedStr) &&
            bool.TryParse(isCompletedStr, out var isCompleted))
        {
            parameters.IsCompleted = isCompleted;
        }

        // Priority filter
        if (query.TryGetValue("priority", out var priorityStr) &&
            Enum.TryParse<Priority>(priorityStr, out var priority))
        {
            parameters.Priority = priority;
        }

        // Category filter
        if (query.TryGetValue("category", out var categoryStr) &&
            Enum.TryParse<Category>(categoryStr, out var category))
        {
            parameters.Category = category;
        }

        // Date range filters
        if (query.TryGetValue("dueDateFrom", out var dueDateFromStr) &&
            DateTime.TryParse(dueDateFromStr, out var dueDateFrom))
        {
            parameters.DueDateFrom = dueDateFrom;
        }

        if (query.TryGetValue("dueDateTo", out var dueDateToStr) &&
            DateTime.TryParse(dueDateToStr, out var dueDateTo))
        {
            parameters.DueDateTo = dueDateTo;
        }

        // Search and tags
        if (query.TryGetValue("searchTerm", out var searchTerm))
        {
            parameters.SearchTerm = searchTerm;
        }

        if (query.TryGetValue("tags", out var tags))
        {
            parameters.Tags = tags;
        }

        // Pagination
        if (query.TryGetValue("page", out var pageStr) &&
            int.TryParse(pageStr, out var page) && page > 0)
        {
            parameters.Page = page;
        }

        if (query.TryGetValue("pageSize", out var pageSizeStr) &&
            int.TryParse(pageSizeStr, out var pageSize) && pageSize > 0)
        {
            parameters.PageSize = pageSize;
        }

        // Sorting
        if (query.TryGetValue("sortBy", out var sortBy) && !string.IsNullOrEmpty(sortBy))
        {
            parameters.SortBy = sortBy.ToString();
        }
        else
        {
            parameters.SortBy = "CreatedAt";
        }

        if (query.TryGetValue("sortDescending", out var sortDescStr) &&
            bool.TryParse(sortDescStr, out var sortDesc))
        {
            parameters.SortDescending = sortDesc;
        }

        return parameters;
    }

    /// <summary>
    /// Extract TodoQueryParameters optimized for export (no pagination)
    /// </summary>
    public static TodoQueryParameters ExtractExportQueryParameters(HttpRequest request)
    {
        var parameters = ExtractTodoQueryParameters(request);

        // Override pagination for export - get all records
        parameters.Page = 1;
        parameters.PageSize = 10000; // Large page size

        // Default sorting for export
        parameters.SortBy = "CreatedAt";
        parameters.SortDescending = false;

        return parameters;
    }
}