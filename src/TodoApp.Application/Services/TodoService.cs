using Microsoft.Extensions.Logging;
using TodoApp.Application.DTOs;
using TodoApp.Application.Common;
using TodoApp.Application.Interfaces;
using TodoApp.Application.Mappers;
using TodoApp.Domain.Entities;
using TodoApp.Domain.Exceptions;

namespace TodoApp.Application.Services;

public class TodoService : ITodoService
{
    private readonly ITodoRepository _todoRepository;
    private readonly ILogger<TodoService> _logger;

    public TodoService(ITodoRepository todoRepository, ILogger<TodoService> logger)
    {
        _todoRepository = todoRepository;
        _logger = logger;
    }

    public async Task<TodoDto> CreateTodoAsync(long userId, CreateTodoRequest request)
    {
        var todoItem = new TodoItem
        {
            Title = request.Title,
            Description = request.Description,
            Priority = request.Priority,
            Category = request.Category,
            DueDate = request.DueDate,
            Tags = request.Tags,
            UserId = userId
        };

        var createdTodo = await _todoRepository.CreateAsync(todoItem);
        return TodoMapper.ToDto(createdTodo);
    }

    public async Task<TodoDto> GetTodoByIdAsync(long userId, long id)
    {
        var todo = await _todoRepository.GetByIdAsync(userId, id);
        if (todo == null)
            throw new TodoNotFoundException(id);
        return TodoMapper.ToDto(todo);
    }

    public async Task<IEnumerable<TodoDto>> GetAllTodosAsync(long userId)
    {
        var todos = await _todoRepository.GetAllAsync(userId);
        return todos.Select(TodoMapper.ToDto);
    }

    public async Task<IEnumerable<TodoDto>> GetCompletedTodosAsync(long userId)
    {
        var todos = await _todoRepository.GetCompletedTodosAsync(userId);
        return todos.Select(TodoMapper.ToDto);
    }

    public async Task<IEnumerable<TodoDto>> GetIncompleteTodosAsync(long userId)
    {
        var todos = await _todoRepository.GetIncompleteTodosAsync(userId);
        return todos.Select(TodoMapper.ToDto);
    }

    public async Task<TodoDto> UpdateTodoAsync(long userId, long id, UpdateTodoRequest request)
    {
        var existingTodo = await _todoRepository.GetByIdAsync(userId, id);
        if (existingTodo == null)
            throw new TodoNotFoundException(id);

        if (!string.IsNullOrEmpty(request.Title))
            existingTodo.Title = request.Title;

        if (request.Description != null)
            existingTodo.Description = request.Description;

        if (request.IsCompleted.HasValue)
            existingTodo.IsCompleted = request.IsCompleted.Value;

        if (request.Priority.HasValue)
            existingTodo.Priority = request.Priority.Value;

        if (request.Category.HasValue)
            existingTodo.Category = request.Category.Value;

        if (request.DueDate.HasValue)
            existingTodo.DueDate = request.DueDate;

        if (request.Tags != null)
            existingTodo.Tags = request.Tags;

        existingTodo.UpdateTimestamp();

        var updatedTodo = await _todoRepository.UpdateAsync(existingTodo);
        return TodoMapper.ToDto(updatedTodo!);
    }

    public async Task<bool> DeleteTodoAsync(long userId, long id)
    {
        var todo = await _todoRepository.GetByIdAsync(userId, id);
        if (todo == null)
            return false;

        await _todoRepository.DeleteAsync(id);
        return true;
    }

    public async Task<bool> TodoExistsAsync(long userId, long id)
    {
        return await _todoRepository.ExistsAsync(userId, id);
    }

    public async Task<PagedResult<TodoDto>> GetTodosAsync(long userId, TodoQueryParameters parameters)
    {
        var pagedResult = await _todoRepository.GetTodosAsync(userId, parameters);
        
        return new PagedResult<TodoDto>
        {
            Items = pagedResult.Items.Select(TodoMapper.ToDto),
            TotalItems = pagedResult.TotalItems,
            PageSize = pagedResult.PageSize
        };
    }

    public async Task<IEnumerable<TodoDto>> SearchTodosAsync(long userId, string searchTerm)
    {
        var todos = await _todoRepository.SearchTodosAsync(userId, searchTerm);
        return todos.Select(TodoMapper.ToDto);
    }

    public async Task<IEnumerable<TodoDto>> GetTodosByTagsAsync(long userId, string tags)
    {
        var todos = await _todoRepository.GetTodosByTagsAsync(userId, tags);
        return todos.Select(TodoMapper.ToDto);
    }

    public async Task<IEnumerable<TodoDto>> GetOverdueTodosAsync(long userId)
    {
        var todos = await _todoRepository.GetOverdueTodosAsync(userId);
        return todos.Select(TodoMapper.ToDto);
    }
}