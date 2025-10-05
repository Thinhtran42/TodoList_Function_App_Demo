using TodoApp.Application.Common;

namespace TodoApp.Application.Extensions;

public static class PaginationExtensions
{
    /// <summary>
    /// Maps one PagedResult to another type using a mapping function
    /// </summary>
    /// <typeparam name="TSource">Source type</typeparam>
    /// <typeparam name="TDestination">Destination type</typeparam>
    /// <param name="source">Source PagedResult</param>
    /// <param name="mapper">Mapping function</param>
    /// <returns>PagedResult of destination type</returns>
    public static PagedResult<TDestination> Map<TSource, TDestination>(
        this PagedResult<TSource> source,
        Func<IEnumerable<TSource>, IEnumerable<TDestination>> mapper)
    {
        return PagedResult<TDestination>.Create(
            mapper(source.Items),
            source.TotalItems,
            source.Page,
            source.PageSize
        );
    }

    /// <summary>
    /// Maps one PagedResult to another type using a mapping function for individual items
    /// </summary>
    /// <typeparam name="TSource">Source type</typeparam>
    /// <typeparam name="TDestination">Destination type</typeparam>
    /// <param name="source">Source PagedResult</param>
    /// <param name="mapper">Mapping function for individual items</param>
    /// <returns>PagedResult of destination type</returns>
    public static PagedResult<TDestination> Map<TSource, TDestination>(
        this PagedResult<TSource> source,
        Func<TSource, TDestination> mapper)
    {
        return PagedResult<TDestination>.Create(
            source.Items.Select(mapper),
            source.TotalItems,
            source.Page,
            source.PageSize
        );
    }
}