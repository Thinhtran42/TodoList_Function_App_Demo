namespace TodoApp.Application.DTOs;

/// <summary>
/// DTOs for Todo CSV import operations
/// </summary>
public class ImportResponse
{
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int TotalRecords { get; set; }
    public int ImportedRecords { get; set; }
    public int FailedRecords { get; set; }
    public List<string> Errors { get; set; } = new();
    public DateTime ImportedAt { get; set; }
}

public class ImportTodoItem
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Priority { get; set; } = "Medium";
    public string Category { get; set; } = "General";
    public string? DueDate { get; set; }
    public string? Tags { get; set; }
    public bool IsCompleted { get; set; } = false;
}

public class ImportValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public CreateTodoRequest? TodoRequest { get; set; }
}
