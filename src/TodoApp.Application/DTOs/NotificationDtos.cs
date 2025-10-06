using TodoApp.Domain.Entities;

namespace TodoApp.Application.DTOs;

public class TodoNotificationMessage
{
    public string EventType { get; set; } = string.Empty; // Created, Updated, Deleted
    public long TodoId { get; set; }
    public string Title { get; set; } = string.Empty;
    public long UserId { get; set; }
    public DateTime Timestamp { get; set; }
    public Priority Priority { get; set; }
    public Category Category { get; set; }
    public bool IsCompleted { get; set; }
}
