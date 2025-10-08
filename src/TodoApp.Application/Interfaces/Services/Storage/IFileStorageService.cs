namespace TodoApp.Application.Interfaces.Services.Storage;

/// <summary>
/// Interface for file storage operations.
/// Supports multiple storage providers (Azure Blob Storage, MinIO S3, etc.)
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Upload a file to storage and return the file URL
    /// </summary>
    /// <param name="fileContent">File content as byte array</param>
    /// <param name="fileName">Name of the file</param>
    /// <param name="containerName">Container/Bucket name (default: "imports")</param>
    /// <returns>File URL for accessing the uploaded file</returns>
    Task<string> UploadFileAsync(byte[] fileContent, string fileName, string containerName = "imports");

    /// <summary>
    /// Download a file from storage
    /// </summary>
    /// <param name="fileUrl">Full file URL (blob URL or S3 URL)</param>
    /// <returns>File content as string</returns>
    Task<string> DownloadFileAsync(string fileUrl);

    /// <summary>
    /// Delete a file from storage
    /// </summary>
    /// <param name="fileUrl">Full file URL to delete</param>
    /// <returns>True if deleted successfully, false otherwise</returns>
    Task<bool> DeleteFileAsync(string fileUrl);

    /// <summary>
    /// Check if a file exists in storage
    /// </summary>
    /// <param name="fileUrl">Full file URL to check</param>
    /// <returns>True if file exists, false otherwise</returns>
    Task<bool> FileExistsAsync(string fileUrl);
}