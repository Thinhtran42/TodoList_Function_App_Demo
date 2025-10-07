namespace TodoApp.Application.Interfaces.Services;

public interface IBlobService
{
    /// <summary>
    /// Upload CSV file to blob storage and return the blob URL
    /// </summary>
    /// <param name="csvContent">CSV file content as byte array</param>
    /// <param name="fileName">Name of the CSV file</param>
    /// <param name="containerName">Blob container name (default: "imports")</param>
    /// <returns>Blob URL for accessing the uploaded file</returns>
    Task<string> UploadCsvAsync(byte[] csvContent, string fileName, string containerName = "imports");

    /// <summary>
    /// Download CSV file from blob storage
    /// </summary>
    /// <param name="blobUrl">Full blob URL</param>
    /// <returns>CSV content as string</returns>
    Task<string> DownloadCsvAsync(string blobUrl);
}