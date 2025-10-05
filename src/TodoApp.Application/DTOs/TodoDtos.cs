using TodoApp.Domain.Entities;
using TodoApp.Application.Common;

namespace TodoApp.Application.DTOs;

public class CreateTodoRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Priority Priority { get; set; } = Priority.Medium;
    public Category Category { get; set; } = Category.General;
    public DateTime? DueDate { get; set; }
    public string? Tags { get; set; }
}

public class UpdateTodoRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public bool? IsCompleted { get; set; }
    public Priority? Priority { get; set; }
    public Category? Category { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Tags { get; set; }
}

public class TodoDto
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsCompleted { get; set; }
    public Priority Priority { get; set; }
    public Category Category { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Tags { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class TodoQueryParameters : BaseQueryParameters
{
    public bool? IsCompleted { get; set; }
    public Priority? Priority { get; set; }
    public Category? Category { get; set; }
    public DateTime? DueDateFrom { get; set; }
    public DateTime? DueDateTo { get; set; }
    public string? Tags { get; set; }
}

public class ExportResponse
{
    public string Message { get; set; } = string.Empty;
    public string? DownloadUrl { get; set; }
    public int ExportedCount { get; set; }
    public DateTime ExportedAt { get; set; }
    public string? FileName { get; set; }
}

// Import DTOs
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