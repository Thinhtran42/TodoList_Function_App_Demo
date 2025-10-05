using TodoApp.Application.DTOs;
using TodoApp.Application.Common;
using TodoApp.Domain.Entities;

namespace TodoApp.Application.Interfaces;

public interface ITodoRepository : IBaseRepository<TodoItem>
{
    Task<TodoItem?> GetByIdAsync(long userId, long id);
    Task<IEnumerable<TodoItem>> GetAllAsync(long userId);
    Task<IEnumerable<TodoItem>> GetCompletedTodosAsync(long userId);
    Task<IEnumerable<TodoItem>> GetIncompleteTodosAsync(long userId);
    Task<PagedResult<TodoItem>> GetTodosAsync(long userId, TodoQueryParameters parameters);
    Task<IEnumerable<TodoItem>> SearchTodosAsync(long userId, string searchTerm);
    Task<IEnumerable<TodoItem>> GetTodosByTagsAsync(long userId, string tags);
    Task<IEnumerable<TodoItem>> GetOverdueTodosAsync(long userId);
    Task<bool> ExistsAsync(long userId, long id);
}