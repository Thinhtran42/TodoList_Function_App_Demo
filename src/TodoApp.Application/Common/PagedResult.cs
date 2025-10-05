namespace TodoApp.Application.Common;

public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = new List<T>();
    public int TotalItems { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Creates a new PagedResult with the provided items and pagination info
    /// </summary>
    public static PagedResult<T> Create(IEnumerable<T> items, int totalItems, int page, int pageSize)
    {
        return new PagedResult<T>
        {
            Items = items,
            TotalItems = totalItems,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// Creates an empty PagedResult
    /// </summary>
    public static PagedResult<T> Empty(int page = 1, int pageSize = 10)
    {
        return new PagedResult<T>
        {
            Items = new List<T>(),
            TotalItems = 0,
            Page = page,
            PageSize = pageSize
        };
    }
}