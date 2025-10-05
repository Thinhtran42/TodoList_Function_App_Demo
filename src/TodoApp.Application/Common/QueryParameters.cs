namespace TodoApp.Application.Common;

public class PaginationParameters
{
    private int _page = 1;
    private int _pageSize = 10;
    private const int MaxPageSize = 100;

    public int Page 
    { 
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize 
    { 
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : (value < 1 ? 1 : value);
    }

    /// <summary>
    /// Calculates the number of items to skip based on current page and page size
    /// </summary>
    public int Skip => (Page - 1) * PageSize;

    /// <summary>
    /// Gets the page size (same as PageSize property, for convenience)
    /// </summary>
    public int Take => PageSize;
}

public class SortingParameters
{
    public string SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
    
    public string SortDirection => SortDescending ? "desc" : "asc";
}

public abstract class BaseQueryParameters : PaginationParameters
{
    public string? SearchTerm { get; set; }
    public string SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
    
    public string SortDirection => SortDescending ? "desc" : "asc";
}