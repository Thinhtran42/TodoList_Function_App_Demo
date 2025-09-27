using Microsoft.Extensions.Logging;
using TodoApp.Application.DTOs;
using TodoApp.Application.Interfaces;
using TodoApp.Domain.Entities;
using TodoApp.Domain.Exceptions;

namespace TodoApp.Application.UseCases;

public class CreateTodoUseCase
{
    private readonly ITodoRepository _todoRepository;
    private readonly ILogger<CreateTodoUseCase> _logger;

    public CreateTodoUseCase(ITodoRepository todoRepository, ILogger<CreateTodoUseCase> logger)
    {
        _todoRepository = todoRepository;
        _logger = logger;
    }

    public async Task<TodoDto> ExecuteAsync(CreateTodoRequest request)
    {
        _logger.LogInformation("Creating new todo with title: {Title}", request.Title);

        try
        {
            var todoItem = TodoItem.Create(request.Title);
            var createdTodo = await _todoRepository.CreateAsync(todoItem);

            _logger.LogInformation("Todo created successfully with ID: {TodoId}", createdTodo.Id);

            return MapToDto(createdTodo);
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

    private static TodoDto MapToDto(TodoItem todoItem)
    {
        return new TodoDto
        {
            Id = todoItem.Id,
            Title = todoItem.Title,
            IsCompleted = todoItem.IsCompleted,
            CreatedAt = todoItem.CreatedAt,
            UpdatedAt = todoItem.UpdatedAt
        };
    }
}