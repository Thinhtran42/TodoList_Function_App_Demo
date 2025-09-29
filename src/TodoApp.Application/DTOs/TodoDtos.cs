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