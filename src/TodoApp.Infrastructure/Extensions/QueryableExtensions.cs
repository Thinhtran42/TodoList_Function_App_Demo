using Microsoft.EntityFrameworkCore;
using TodoApp.Application.Common;

namespace TodoApp.Infrastructure.Extensions;

public static class QueryableExtensions
{
    /// <summary>
    /// Applies pagination to an IQueryable and returns a PagedResult
    /// </summary>
    /// <typeparam name="T">The type of entity</typeparam>
    /// <param name="query">The queryable source</param>
    /// <param name="paginationParams">Pagination parameters</param>
    /// <returns>A PagedResult containing the requested page of data</returns>
    public static async Task<PagedResult<T>> ToPaginatedResultAsync<T>(
        this IQueryable<T> query, 
        PaginationParameters paginationParams)
    {
        var totalItems = await query.CountAsync();
        
        var items = await query
            .Skip(paginationParams.Skip)
            .Take(paginationParams.Take)
            .ToListAsync();

        return PagedResult<T>.Create(items, totalItems, paginationParams.Page, paginationParams.PageSize);
    }

    /// <summary>
    /// Applies pagination to an IQueryable and returns a PagedResult
    /// </summary>
    /// <typeparam name="T">The type of entity</typeparam>
    /// <param name="query">The queryable source</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>A PagedResult containing the requested page of data</returns>
    public static async Task<PagedResult<T>> ToPaginatedResultAsync<T>(
        this IQueryable<T> query, 
        int page = 1, 
        int pageSize = 10)
    {
        var paginationParams = new PaginationParameters { Page = page, PageSize = pageSize };
        return await query.ToPaginatedResultAsync(paginationParams);
    }
}