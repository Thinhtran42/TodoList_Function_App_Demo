namespace TodoApp.Application.DTOs;

/// <summary>
/// DTOs for Todo messaging operations (Service Bus, RabbitMQ, etc.)
/// </summary>
public class ImportMessage
{
    public string UserId { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string RequestId { get; set; } = Guid.NewGuid().ToString();
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
}
