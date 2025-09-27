using Microsoft.Extensions.Logging;
using TodoApp.Application.DTOs;
using TodoApp.Application.Interfaces;
using TodoApp.Domain.Exceptions;

namespace TodoApp.Application.UseCases;

public class UpdateTodoUseCase
{
    private readonly ITodoRepository _todoRepository;
    private readonly ILogger<UpdateTodoUseCase> _logger;

    public UpdateTodoUseCase(ITodoRepository todoRepository, ILogger<UpdateTodoUseCase> logger)
    {
        _todoRepository = todoRepository;
        _logger = logger;
    }

    public async Task<TodoDto> ExecuteAsync(long id, UpdateTodoRequest request)
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

            var updatedTodo = await _todoRepository.UpdateAsync(todoItem);
            if (updatedTodo == null)
            {
                throw new TodoNotFoundException(id);
            }

            _logger.LogInformation("Todo updated successfully with ID: {TodoId}", id);

            return new TodoDto
            {
                Id = updatedTodo.Id,
                Title = updatedTodo.Title,
                IsCompleted = updatedTodo.IsCompleted,
                CreatedAt = updatedTodo.CreatedAt,
                UpdatedAt = updatedTodo.UpdatedAt
            };
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
}