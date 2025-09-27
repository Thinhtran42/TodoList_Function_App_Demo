using Microsoft.Extensions.Logging;
using TodoApp.Application.Interfaces;
using TodoApp.Domain.Exceptions;

namespace TodoApp.Application.UseCases;

public class DeleteTodoUseCase
{
    private readonly ITodoRepository _todoRepository;
    private readonly ILogger<DeleteTodoUseCase> _logger;

    public DeleteTodoUseCase(ITodoRepository todoRepository, ILogger<DeleteTodoUseCase> logger)
    {
        _todoRepository = todoRepository;
        _logger = logger;
    }

    public async Task<bool> ExecuteAsync(long id)
    {
        _logger.LogInformation("Deleting todo with ID: {TodoId}", id);

        var exists = await _todoRepository.ExistsAsync(id);
        if (!exists)
        {
            _logger.LogWarning("Todo not found with ID: {TodoId}", id);
            throw new TodoNotFoundException(id);
        }

        var deleted = await _todoRepository.DeleteAsync(id);
        if (deleted)
        {
            _logger.LogInformation("Todo deleted successfully with ID: {TodoId}", id);
        }
        else
        {
            _logger.LogError("Failed to delete todo with ID: {TodoId}", id);
        }

        return deleted;
    }
}