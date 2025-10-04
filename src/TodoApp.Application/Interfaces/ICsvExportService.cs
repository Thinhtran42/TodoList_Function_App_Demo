using TodoApp.Application.DTOs;

namespace TodoApp.Application.Interfaces;

public interface ICsvExportService
{
    Task<byte[]> ExportTodosToCsvAsync(IEnumerable<TodoDto> todos);
    Task<string> UploadCsvToBlobAsync(byte[] csvData, string fileName);
}