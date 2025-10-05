using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using TodoApp.Application.DTOs;
using TodoApp.Application.Common;
using TodoApp.Application.Interfaces;
using TodoApp.Domain.Entities;
using TodoApp.Infrastructure.Data;
using TodoApp.Infrastructure.Extensions;

namespace TodoApp.Infrastructure.Repositories;

public class TodoRepository : BaseRepository<TodoItem>, ITodoRepository
{
    public TodoRepository(TodoDbContext context) : base(context)
    {
    }

    // User-specific methods
    public async Task<TodoItem?> GetByIdAsync(long userId, long id)
    {
        return await DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
    }

    public async Task<IEnumerable<TodoItem>> GetAllAsync(long userId)
    {
        return await DbSet
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.Id)
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(long userId, long id)
    {
        return await DbSet.AnyAsync(x => x.Id == id && x.UserId == userId);
    }

    public async Task<IEnumerable<TodoItem>> GetCompletedTodosAsync(long userId)
    {
        return await DbSet
            .AsNoTracking()
            .Where(x => x.IsCompleted && x.UserId == userId)
            .OrderByDescending(x => x.Id)
            .ToListAsync();
    }

    public async Task<IEnumerable<TodoItem>> GetIncompleteTodosAsync(long userId)
    {
        return await DbSet
            .AsNoTracking()
            .Where(x => !x.IsCompleted && x.UserId == userId)
            .OrderByDescending(x => x.Id)
            .ToListAsync();
    }

    // Override UpdateAsync to use domain methods for better business logic
    public override async Task<TodoItem?> UpdateAsync(TodoItem todoItem)
    {
        var existingTodo = await DbSet.FindAsync(todoItem.Id);
        if (existingTodo == null)
        {
            return null;
        }

        // Use domain methods for business logic
        if (existingTodo.Title != todoItem.Title)
        {
            existingTodo.UpdateTitle(todoItem.Title);
        }

        if (existingTodo.IsCompleted != todoItem.IsCompleted)
        {
            if (todoItem.IsCompleted)
            {
                existingTodo.MarkAsCompleted();
            }
            else
            {
                existingTodo.MarkAsIncomplete();
            }
        }

        await Context.SaveChangesAsync();
        return existingTodo;
    }

    public async Task<TodoApp.Application.Common.PagedResult<TodoItem>> GetTodosAsync(long userId, TodoQueryParameters parameters)
    {
        var query = DbSet.AsNoTracking().Where(x => x.UserId == userId);

        // Apply filters
        if (parameters.IsCompleted.HasValue)
        {
            query = query.Where(x => x.IsCompleted == parameters.IsCompleted.Value);
        }

        if (parameters.Priority.HasValue)
        {
            query = query.Where(x => x.Priority == parameters.Priority.Value);
        }

        if (parameters.Category.HasValue)
        {
            query = query.Where(x => x.Category == parameters.Category.Value);
        }

        if (parameters.DueDateFrom.HasValue)
        {
            query = query.Where(x => x.DueDate >= parameters.DueDateFrom.Value);
        }

        if (parameters.DueDateTo.HasValue)
        {
            query = query.Where(x => x.DueDate <= parameters.DueDateTo.Value);
        }

        if (!string.IsNullOrEmpty(parameters.SearchTerm))
        {
            var searchTerm = parameters.SearchTerm.ToLower();
            query = query.Where(x => 
                x.Title.ToLower().Contains(searchTerm) ||
                (x.Description != null && x.Description.ToLower().Contains(searchTerm)));
        }

        if (!string.IsNullOrEmpty(parameters.Tags))
        {
            var tags = parameters.Tags.ToLower();
            query = query.Where(x => x.Tags != null && x.Tags.ToLower().Contains(tags));
        }

        // Get total count before pagination
        var totalItems = await query.CountAsync();

        // Apply sorting
        var sortBy = parameters.SortBy ?? "CreatedAt";
        var sortDirection = parameters.SortDescending ? "desc" : "asc";
        query = query.OrderBy($"{sortBy} {sortDirection}");

        // Apply pagination using extension method
        return await query.ToPaginatedResultAsync(parameters);
    }

    public async Task<IEnumerable<TodoItem>> SearchTodosAsync(long userId, string searchTerm)
    {
        if (string.IsNullOrEmpty(searchTerm))
        {
            return new List<TodoItem>();
        }

        var lowerSearchTerm = searchTerm.ToLower();
        return await DbSet
            .AsNoTracking()
            .Where(x => x.UserId == userId && (
                x.Title.ToLower().Contains(lowerSearchTerm) ||
                (x.Description != null && x.Description.ToLower().Contains(lowerSearchTerm)) ||
                (x.Tags != null && x.Tags.ToLower().Contains(lowerSearchTerm))))
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<TodoItem>> GetTodosByTagsAsync(long userId, string tags)
    {
        if (string.IsNullOrEmpty(tags))
        {
            return new List<TodoItem>();
        }

        var lowerTags = tags.ToLower();
        return await DbSet
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Tags != null && x.Tags.ToLower().Contains(lowerTags))
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<TodoItem>> GetOverdueTodosAsync(long userId)
    {
        var now = DateTime.UtcNow;
        return await DbSet
            .AsNoTracking()
            .Where(x => x.UserId == userId && !x.IsCompleted && x.DueDate.HasValue && x.DueDate.Value < now)
            .OrderBy(x => x.DueDate)
            .ToListAsync();
    }
}