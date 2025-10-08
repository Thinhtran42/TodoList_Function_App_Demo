using TodoApp.Application.DTOs;

namespace TodoApp.Application.Interfaces.Services.DataProcessing;

public interface ICsvExportService
{
    Task<byte[]> ExportTodosToCsvAsync(IEnumerable<TodoDto> todos);

    /// <summary>
    /// Upload CSV to configured storage provider (Azure Blob, MinIO, etc.)
    /// </summary>
    Task<FileUploadResult> UploadCsvToStorageAsync(byte[] csvData, string fileName);
}