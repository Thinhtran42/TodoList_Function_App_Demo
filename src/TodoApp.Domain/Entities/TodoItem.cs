namespace TodoApp.Domain.Entities;

public class TodoItem : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsCompleted { get; set; }
    public Priority Priority { get; set; } = Priority.Medium;
    public Category Category { get; set; } = Category.General;
    public DateTime? DueDate { get; set; }
    public string? Tags { get; set; }

    // Domain business rules
    public void MarkAsCompleted()
    {
        if (IsCompleted)
            throw new InvalidOperationException("Todo is already completed");
            
        IsCompleted = true;
        UpdateTimestamp();
    }

    public void MarkAsIncomplete()
    {
        if (!IsCompleted)
            throw new InvalidOperationException("Todo is already incomplete");
            
        IsCompleted = false;
        UpdateTimestamp();
    }

    public void UpdateTitle(string newTitle)
    {
        if (string.IsNullOrWhiteSpace(newTitle))
            throw new ArgumentException("Title cannot be empty", nameof(newTitle));
            
        if (newTitle.Length > 500)
            throw new ArgumentException("Title cannot exceed 500 characters", nameof(newTitle));
            
        Title = newTitle.Trim();
        UpdateTimestamp();
    }

    public void UpdateDescription(string? description)
    {
        if (description != null && description.Length > 2000)
            throw new ArgumentException("Description cannot exceed 2000 characters", nameof(description));
            
        Description = description?.Trim();
        UpdateTimestamp();
    }

    public void SetPriority(Priority priority)
    {
        Priority = priority;
        UpdateTimestamp();
    }

    public void SetCategory(Category category)
    {
        Category = category;
        UpdateTimestamp();
    }

    public void SetDueDate(DateTime? dueDate)
    {
        if (dueDate.HasValue && dueDate.Value < DateTime.UtcNow.Date)
            throw new ArgumentException("Due date cannot be in the past", nameof(dueDate));
            
        DueDate = dueDate;
        UpdateTimestamp();
    }

    public void SetTags(string? tags)
    {
        if (tags != null && tags.Length > 500)
            throw new ArgumentException("Tags cannot exceed 500 characters", nameof(tags));
            
        Tags = tags?.Trim();
        UpdateTimestamp();
    }

    public bool IsOverdue()
    {
        return DueDate.HasValue && DueDate.Value < DateTime.UtcNow && !IsCompleted;
    }

    public static TodoItem Create(string title, string? description = null, Priority priority = Priority.Medium, Category category = Category.General)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));
            
        if (title.Length > 500)
            throw new ArgumentException("Title cannot exceed 500 characters", nameof(title));

        return new TodoItem
        {
            Title = title.Trim(),
            Description = description?.Trim(),
            IsCompleted = false,
            Priority = priority,
            Category = category
        };
    }
}