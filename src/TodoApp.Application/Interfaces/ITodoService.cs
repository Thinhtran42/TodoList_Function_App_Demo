using TodoApp.Application.DTOs;
using TodoApp.Application.Common;

namespace TodoApp.Application.Interfaces;

public interface ITodoService
{
    Task<TodoDto> CreateTodoAsync(CreateTodoRequest request);
    Task<TodoDto> GetTodoByIdAsync(long id);
    Task<IEnumerable<TodoDto>> GetAllTodosAsync();
    Task<IEnumerable<TodoDto>> GetCompletedTodosAsync();
    Task<IEnumerable<TodoDto>> GetIncompleteTodosAsync();
    Task<TodoDto> UpdateTodoAsync(long id, UpdateTodoRequest request);
    Task<bool> DeleteTodoAsync(long id);
    Task<bool> TodoExistsAsync(long id);
    Task<PagedResult<TodoDto>> GetTodosAsync(TodoQueryParameters parameters);
    Task<IEnumerable<TodoDto>> SearchTodosAsync(string searchTerm);
    Task<IEnumerable<TodoDto>> GetTodosByTagsAsync(string tags);
    Task<IEnumerable<TodoDto>> GetOverdueTodosAsync();
}