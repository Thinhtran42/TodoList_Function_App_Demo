namespace TodoApp.Domain.Entities;

public class TodoItem
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Domain business rules
    public void MarkAsCompleted()
    {
        if (IsCompleted)
            throw new InvalidOperationException("Todo is already completed");
            
        IsCompleted = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsIncomplete()
    {
        if (!IsCompleted)
            throw new InvalidOperationException("Todo is already incomplete");
            
        IsCompleted = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateTitle(string newTitle)
    {
        if (string.IsNullOrWhiteSpace(newTitle))
            throw new ArgumentException("Title cannot be empty", nameof(newTitle));
            
        if (newTitle.Length > 500)
            throw new ArgumentException("Title cannot exceed 500 characters", nameof(newTitle));
            
        Title = newTitle.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public static TodoItem Create(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));
            
        if (title.Length > 500)
            throw new ArgumentException("Title cannot exceed 500 characters", nameof(title));

        return new TodoItem
        {
            Title = title.Trim(),
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}