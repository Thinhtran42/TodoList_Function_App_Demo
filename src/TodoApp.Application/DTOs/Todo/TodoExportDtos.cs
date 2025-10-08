namespace TodoApp.Application.DTOs;

/// <summary>
/// DTOs for Todo export and blob storage operations
/// </summary>
public class ExportResponse
{
    public string Message { get; set; } = string.Empty;
    public string? DownloadUrl { get; set; }
    public int ExportedCount { get; set; }
    public DateTime ExportedAt { get; set; }
    public string? FileName { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string AccessPermissions { get; set; } = "Read";
    public int ValidForMinutes { get; set; }
    public string StorageInfo { get; set; } = string.Empty;
}

/// <summary>
/// Result of file upload operation to storage (Azure Blob, MinIO, etc.)
/// </summary>
public class FileUploadResult
{
    public string DownloadUrl { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string Permissions { get; set; } = "Read";
    public string ContainerName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
}
