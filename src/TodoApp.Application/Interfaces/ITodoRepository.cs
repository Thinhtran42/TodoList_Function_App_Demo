using TodoApp.Domain.Entities;

namespace TodoApp.Application.Interfaces;

public interface ITodoRepository
{
    Task<TodoItem?> GetByIdAsync(long id);
    Task<IEnumerable<TodoItem>> GetAllAsync();
    Task<TodoItem> CreateAsync(TodoItem todoItem);
    Task<TodoItem?> UpdateAsync(TodoItem todoItem);
    Task<bool> DeleteAsync(long id);
    Task<bool> ExistsAsync(long id);
}