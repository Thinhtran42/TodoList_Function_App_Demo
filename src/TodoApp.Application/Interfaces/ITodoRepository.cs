using TodoApp.Application.DTOs;
using TodoApp.Application.Common;
using TodoApp.Domain.Entities;

namespace TodoApp.Application.Interfaces;

public interface ITodoRepository : IBaseRepository<TodoItem>
{
    Task<IEnumerable<TodoItem>> GetCompletedTodosAsync();
    Task<IEnumerable<TodoItem>> GetIncompleteTodosAsync();
    Task<PagedResult<TodoItem>> GetTodosAsync(TodoQueryParameters parameters);
    Task<IEnumerable<TodoItem>> SearchTodosAsync(string searchTerm);
    Task<IEnumerable<TodoItem>> GetTodosByTagsAsync(string tags);
    Task<IEnumerable<TodoItem>> GetOverdueTodosAsync();
}