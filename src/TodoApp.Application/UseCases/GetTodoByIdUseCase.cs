using Microsoft.Extensions.Logging;
using TodoApp.Application.DTOs;
using TodoApp.Application.Interfaces;
using TodoApp.Domain.Exceptions;

namespace TodoApp.Application.UseCases;

public class GetTodoByIdUseCase
{
    private readonly ITodoRepository _todoRepository;
    private readonly ILogger<GetTodoByIdUseCase> _logger;

    public GetTodoByIdUseCase(ITodoRepository todoRepository, ILogger<GetTodoByIdUseCase> logger)
    {
        _todoRepository = todoRepository;
        _logger = logger;
    }

    public async Task<TodoDto> ExecuteAsync(long id)
    {
        _logger.LogInformation("Getting todo by ID: {TodoId}", id);

        var todoItem = await _todoRepository.GetByIdAsync(id);
        if (todoItem == null)
        {
            _logger.LogWarning("Todo not found with ID: {TodoId}", id);
            throw new TodoNotFoundException(id);
        }

        _logger.LogDebug("Found todo with ID: {TodoId}", id);

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