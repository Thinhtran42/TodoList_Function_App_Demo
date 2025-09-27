namespace TodoApp.Application.DTOs;

public class CreateTodoRequest
{
    public string Title { get; set; } = string.Empty;
}

public class UpdateTodoRequest
{
    public string? Title { get; set; }
    public bool? IsCompleted { get; set; }
}

public class TodoDto
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}