using Microsoft.EntityFrameworkCore;
using TodoApp.Application.Interfaces;
using TodoApp.Domain.Entities;
using TodoApp.Infrastructure.Data;

namespace TodoApp.Infrastructure.Repositories;

public class TodoRepository : ITodoRepository
{
    private readonly TodoDbContext _context;

    public TodoRepository(TodoDbContext context)
    {
        _context = context;
    }

    public async Task<TodoItem?> GetByIdAsync(long id)
    {
        return await _context.TodoItems
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<IEnumerable<TodoItem>> GetAllAsync()
    {
        return await _context.TodoItems
            .AsNoTracking()
            .OrderByDescending(x => x.Id)
            .ToListAsync();
    }

    public async Task<TodoItem> CreateAsync(TodoItem todoItem)
    {
        _context.TodoItems.Add(todoItem);
        await _context.SaveChangesAsync();
        return todoItem;
    }

    public async Task<TodoItem?> UpdateAsync(TodoItem todoItem)
    {
        var existingTodo = await _context.TodoItems.FindAsync(todoItem.Id);
        if (existingTodo == null)
        {
            return null;
        }

        existingTodo.Title = todoItem.Title;
        existingTodo.IsCompleted = todoItem.IsCompleted;
        existingTodo.UpdatedAt = todoItem.UpdatedAt;

        _context.TodoItems.Update(existingTodo);
        await _context.SaveChangesAsync();
        return existingTodo;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var todoItem = await _context.TodoItems.FindAsync(id);
        if (todoItem == null)
        {
            return false;
        }

        _context.TodoItems.Remove(todoItem);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(long id)
    {
        return await _context.TodoItems.AnyAsync(x => x.Id == id);
    }
}