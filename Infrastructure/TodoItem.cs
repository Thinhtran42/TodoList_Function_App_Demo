using System;
using System.Collections.Generic;

namespace ToDoFunction.Infrastructure;

public partial class TodoItem
{
    public long Id { get; set; }

    public string Title { get; set; } = null!;

    public bool IsCompleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
