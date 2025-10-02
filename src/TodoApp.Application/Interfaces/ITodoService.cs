using TodoApp.Application.DTOs;
using TodoApp.Application.Common;

namespace TodoApp.Application.Interfaces;

public interface ITodoService
{
    Task<TodoDto> CreateTodoAsync(long userId, CreateTodoRequest request);
    Task<TodoDto> GetTodoByIdAsync(long userId, long id);
    Task<IEnumerable<TodoDto>> GetAllTodosAsync(long userId);
    Task<IEnumerable<TodoDto>> GetCompletedTodosAsync(long userId);
    Task<IEnumerable<TodoDto>> GetIncompleteTodosAsync(long userId);
    Task<TodoDto> UpdateTodoAsync(long userId, long id, UpdateTodoRequest request);
    Task<bool> DeleteTodoAsync(long userId, long id);
    Task<bool> TodoExistsAsync(long userId, long id);
    Task<PagedResult<TodoDto>> GetTodosAsync(long userId, TodoQueryParameters parameters);
    Task<IEnumerable<TodoDto>> SearchTodosAsync(long userId, string searchTerm);
    Task<IEnumerable<TodoDto>> GetTodosByTagsAsync(long userId, string tags);
    Task<IEnumerable<TodoDto>> GetOverdueTodosAsync(long userId);
}