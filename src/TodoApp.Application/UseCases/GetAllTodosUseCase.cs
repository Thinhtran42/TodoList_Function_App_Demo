using Microsoft.Extensions.Logging;
using TodoApp.Application.DTOs;
using TodoApp.Application.Interfaces;

namespace TodoApp.Application.UseCases;

public class GetAllTodosUseCase
{
    private readonly ITodoRepository _todoRepository;
    private readonly ILogger<GetAllTodosUseCase> _logger;

    public GetAllTodosUseCase(ITodoRepository todoRepository, ILogger<GetAllTodosUseCase> logger)
    {
        _todoRepository = todoRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<TodoDto>> ExecuteAsync()
    {
        _logger.LogInformation("Getting all todos");

        var todoItems = await _todoRepository.GetAllAsync();
        
        var result = todoItems.Select(todo => new TodoDto
        {
            Id = todo.Id,
            Title = todo.Title,
            IsCompleted = todo.IsCompleted,
            CreatedAt = todo.CreatedAt,
            UpdatedAt = todo.UpdatedAt
        }).OrderByDescending(x => x.Id);

        _logger.LogDebug("Found {Count} todos", result.Count());

        return result;
    }
}