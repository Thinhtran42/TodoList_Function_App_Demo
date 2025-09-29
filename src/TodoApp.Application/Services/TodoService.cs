using Microsoft.Extensions.Logging;
using TodoApp.Application.DTOs;
using TodoApp.Application.Common;
using TodoApp.Application.Extensions;
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

    public async Task<TodoDto> CreateTodoAsync(CreateTodoRequest request)
    {
        _logger.LogInformation("Creating new todo with title: {Title}", request.Title);

        try
        {
            var todoItem = TodoItem.Create(request.Title, request.Description, request.Priority, request.Category);
            
            if (request.DueDate.HasValue)
            {
                todoItem.SetDueDate(request.DueDate);
            }
            
            if (!string.IsNullOrEmpty(request.Tags))
            {
                todoItem.SetTags(request.Tags);
            }

            var createdTodo = await _todoRepository.CreateAsync(todoItem);

            _logger.LogInformation("Todo created successfully with ID: {TodoId}", createdTodo.Id);
            return TodoMapper.ToDto(createdTodo);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid todo creation request: {Message}", ex.Message);
            throw new TodoValidationException(ex.Message, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating todo with title: {Title}", request.Title);
            throw;
        }
    }

    public async Task<TodoDto> GetTodoByIdAsync(long id)
    {
        _logger.LogInformation("Getting todo by ID: {TodoId}", id);

        var todoItem = await _todoRepository.GetByIdAsync(id);
        if (todoItem == null)
        {
            _logger.LogWarning("Todo not found with ID: {TodoId}", id);
            throw new TodoNotFoundException(id);
        }

        _logger.LogDebug("Found todo with ID: {TodoId}", id);
        return TodoMapper.ToDto(todoItem);
    }

    public async Task<IEnumerable<TodoDto>> GetAllTodosAsync()
    {
        _logger.LogInformation("Getting all todos");

        var todoItems = await _todoRepository.GetAllAsync();
        var result = TodoMapper.ToDto(todoItems);

        _logger.LogDebug("Found {Count} todos", result.Count());
        return result;
    }

    public async Task<IEnumerable<TodoDto>> GetCompletedTodosAsync()
    {
        _logger.LogInformation("Getting completed todos");

        var todoItems = await _todoRepository.GetCompletedTodosAsync();
        var result = TodoMapper.ToDto(todoItems);

        _logger.LogDebug("Found {Count} completed todos", result.Count());
        return result;
    }

    public async Task<IEnumerable<TodoDto>> GetIncompleteTodosAsync()
    {
        _logger.LogInformation("Getting incomplete todos");

        var todoItems = await _todoRepository.GetIncompleteTodosAsync();
        var result = TodoMapper.ToDto(todoItems);

        _logger.LogDebug("Found {Count} incomplete todos", result.Count());
        return result;
    }

    public async Task<TodoDto> UpdateTodoAsync(long id, UpdateTodoRequest request)
    {
        _logger.LogInformation("Updating todo with ID: {TodoId}", id);

        var todoItem = await _todoRepository.GetByIdAsync(id);
        if (todoItem == null)
        {
            _logger.LogWarning("Todo not found with ID: {TodoId}", id);
            throw new TodoNotFoundException(id);
        }

        try
        {
            // Apply updates based on what's provided
            if (!string.IsNullOrWhiteSpace(request.Title))
            {
                todoItem.UpdateTitle(request.Title);
                _logger.LogDebug("Updated title for todo {TodoId}", id);
            }

            if (!string.IsNullOrEmpty(request.Description))
            {
                todoItem.UpdateDescription(request.Description);
                _logger.LogDebug("Updated description for todo {TodoId}", id);
            }

            if (request.IsCompleted.HasValue)
            {
                if (request.IsCompleted.Value)
                {
                    todoItem.MarkAsCompleted();
                    _logger.LogDebug("Marked todo {TodoId} as completed", id);
                }
                else
                {
                    todoItem.MarkAsIncomplete();
                    _logger.LogDebug("Marked todo {TodoId} as incomplete", id);
                }
            }

            if (request.Priority.HasValue)
            {
                todoItem.SetPriority(request.Priority.Value);
                _logger.LogDebug("Updated priority for todo {TodoId}", id);
            }

            if (request.Category.HasValue)
            {
                todoItem.SetCategory(request.Category.Value);
                _logger.LogDebug("Updated category for todo {TodoId}", id);
            }

            if (request.DueDate.HasValue)
            {
                todoItem.SetDueDate(request.DueDate);
                _logger.LogDebug("Updated due date for todo {TodoId}", id);
            }

            if (request.Tags != null)
            {
                todoItem.SetTags(request.Tags);
                _logger.LogDebug("Updated tags for todo {TodoId}", id);
            }

            var updatedTodo = await _todoRepository.UpdateAsync(todoItem);
            if (updatedTodo == null)
            {
                throw new TodoNotFoundException(id);
            }

            _logger.LogInformation("Todo updated successfully with ID: {TodoId}", id);
            return TodoMapper.ToDto(updatedTodo);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid todo update request for ID {TodoId}: {Message}", id, ex.Message);
            throw new TodoValidationException(ex.Message, ex);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Invalid operation for todo {TodoId}: {Message}", id, ex.Message);
            throw new TodoValidationException(ex.Message, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating todo with ID: {TodoId}", id);
            throw;
        }
    }

    public async Task<bool> DeleteTodoAsync(long id)
    {
        _logger.LogInformation("Deleting todo with ID: {TodoId}", id);

        try
        {
            var result = await _todoRepository.DeleteAsync(id);
            
            if (result)
            {
                _logger.LogInformation("Todo deleted successfully with ID: {TodoId}", id);
            }
            else
            {
                _logger.LogWarning("Todo not found for deletion with ID: {TodoId}", id);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting todo with ID: {TodoId}", id);
            throw;
        }
    }

    public async Task<bool> TodoExistsAsync(long id)
    {
        _logger.LogDebug("Checking if todo exists with ID: {TodoId}", id);
        return await _todoRepository.ExistsAsync(id);
    }

    public async Task<PagedResult<TodoDto>> GetTodosAsync(TodoQueryParameters parameters)
    {
        _logger.LogInformation("Getting todos with parameters: Page={Page}, PageSize={PageSize}", parameters.Page, parameters.PageSize);

        try
        {
            var result = await _todoRepository.GetTodosAsync(parameters);

            _logger.LogDebug("Found {Count} todos out of {Total} total", result.Items.Count(), result.TotalItems);

            return result.Map((Func<IEnumerable<TodoItem>, IEnumerable<TodoDto>>)TodoMapper.ToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting todos");
            throw;
        }
    }

    public async Task<IEnumerable<TodoDto>> SearchTodosAsync(string searchTerm)
    {
        _logger.LogInformation("Searching todos with term: {SearchTerm}", searchTerm);

        var todoItems = await _todoRepository.SearchTodosAsync(searchTerm);
        var result = TodoMapper.ToDto(todoItems);

        _logger.LogDebug("Found {Count} todos matching search term", result.Count());
        return result;
    }

    public async Task<IEnumerable<TodoDto>> GetTodosByTagsAsync(string tags)
    {
        _logger.LogInformation("Getting todos by tags: {Tags}", tags);

        var todoItems = await _todoRepository.GetTodosByTagsAsync(tags);
        var result = TodoMapper.ToDto(todoItems);

        _logger.LogDebug("Found {Count} todos with tags", result.Count());
        return result;
    }

    public async Task<IEnumerable<TodoDto>> GetOverdueTodosAsync()
    {
        _logger.LogInformation("Getting overdue todos");

        var todoItems = await _todoRepository.GetOverdueTodosAsync();
        var result = TodoMapper.ToDto(todoItems);

        _logger.LogDebug("Found {Count} overdue todos", result.Count());
        return result;
    }
}