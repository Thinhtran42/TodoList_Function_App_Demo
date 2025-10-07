namespace TodoApp.Application.DTOs;

/// <summary>
/// DTOs for Todo ServiceBus messaging operations
/// </summary>
public class ImportMessage
{
    public string UserId { get; set; } = string.Empty;
    public string BlobUrl { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string RequestId { get; set; } = Guid.NewGuid().ToString();
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
}
