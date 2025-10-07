using TodoApp.Application.DTOs;

namespace TodoApp.Application.Interfaces;

public interface ICsvExportService
{
    Task<byte[]> ExportTodosToCsvAsync(IEnumerable<TodoDto> todos);
    Task<BlobUploadResult> UploadCsvToBlobAsync(byte[] csvData, string fileName);
}