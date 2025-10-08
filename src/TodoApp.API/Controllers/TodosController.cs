using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoApp.API.Helpers;
using TodoApp.Application.Common;
using TodoApp.Application.DTOs;
using TodoApp.Application.Interfaces.Services;
using TodoApp.Domain.Exceptions;

namespace TodoApp.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TodosController : ControllerBase
{
    private readonly ILogger<TodosController> _logger;
    private readonly ITodoService _todoService;
    private readonly IValidator<CreateTodoRequest> _createTodoValidator;
    private readonly IValidator<UpdateTodoRequest> _updateTodoValidator;
    private readonly IValidator<TodoQueryParameters> _queryParametersValidator;

    public TodosController(
        ILogger<TodosController> logger,
        ITodoService todoService,
        IValidator<CreateTodoRequest> createTodoValidator,
        IValidator<UpdateTodoRequest> updateTodoValidator,
        IValidator<TodoQueryParameters> queryParametersValidator)
    {
        _logger = logger;
        _todoService = todoService;
        _createTodoValidator = createTodoValidator;
        _updateTodoValidator = updateTodoValidator;
        _queryParametersValidator = queryParametersValidator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(TodoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateTodo([FromBody] CreateTodoRequest request)
    {
        try
        {
            _logger.LogInformation("Creating new todo");

            // Use AuthHelper to get user ID
            var userId = AuthHelper.GetUserIdOrThrow(HttpContext);

            // Use ValidationHelper to validate request
            var (isValid, errors) = await ValidationHelper.ValidateAndGetErrorsAsync(_createTodoValidator, request);
            if (!isValid)
            {
                return BadRequest(errors);
            }

            var todo = await _todoService.CreateTodoAsync(userId, request);
            return CreatedAtAction(nameof(GetTodoById), new { id = todo.Id }, todo);
        }
        catch (TodoValidationException ex)
        {
            _logger.LogWarning("Validation error: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized: {Message}", ex.Message);
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating todo");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TodoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetTodos()
    {
        try
        {
            _logger.LogInformation("Getting todos with query parameters");

            // Use AuthHelper to get user ID
            var userId = AuthHelper.GetUserIdOrThrow(HttpContext);

            // Use QueryParameterHelper to extract query parameters from request
            var queryParams = QueryParameterHelper.ExtractTodoQueryParameters(Request);

            // Use ValidationHelper to validate query parameters
            var (isValid, errors) = await ValidationHelper.ValidateAndGetErrorsAsync(_queryParametersValidator, queryParams);
            if (!isValid)
            {
                return BadRequest(errors);
            }

            var todos = await _todoService.GetTodosAsync(userId, queryParams);
            return Ok(todos);
        }
        catch (TodoValidationException ex)
        {
            _logger.LogWarning("Validation error in query parameters: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized: {Message}", ex.Message);
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting todos");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TodoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetTodoById(long id)
    {
        try
        {
            _logger.LogInformation("Getting todo by id: {Id}", id);

            // Use AuthHelper to get user ID
            var userId = AuthHelper.GetUserIdOrThrow(HttpContext);
            var todo = await _todoService.GetTodoByIdAsync(userId, id);
            return Ok(todo);
        }
        catch (TodoNotFoundException ex)
        {
            _logger.LogWarning("Todo not found: {Message}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized: {Message}", ex.Message);
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting todo");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(TodoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateTodo(long id, [FromBody] UpdateTodoRequest request)
    {
        try
        {
            _logger.LogInformation("Updating todo: {Id}", id);

            // Use AuthHelper to get user ID
            var userId = AuthHelper.GetUserIdOrThrow(HttpContext);

            // Use ValidationHelper to validate request
            var (isValid, errors) = await ValidationHelper.ValidateAndGetErrorsAsync(_updateTodoValidator, request);
            if (!isValid)
            {
                return BadRequest(errors);
            }

            var todo = await _todoService.UpdateTodoAsync(userId, id, request);
            return Ok(todo);
        }
        catch (TodoNotFoundException ex)
        {
            _logger.LogWarning("Todo not found: {Message}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
        catch (TodoValidationException ex)
        {
            _logger.LogWarning("Validation error: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized: {Message}", ex.Message);
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating todo");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteTodo(long id)
    {
        try
        {
            _logger.LogInformation("Deleting todo: {Id}", id);

            // Use AuthHelper to get user ID
            var userId = AuthHelper.GetUserIdOrThrow(HttpContext);
            await _todoService.DeleteTodoAsync(userId, id);
            return NoContent();
        }
        catch (TodoNotFoundException ex)
        {
            _logger.LogWarning("Todo not found: {Message}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized: {Message}", ex.Message);
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting todo");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPatch("{id}/complete")]
    [ProducesResponseType(typeof(TodoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CompleteTodo(long id)
    {
        try
        {
            _logger.LogInformation("Completing todo: {Id}", id);

            var userId = AuthHelper.GetUserIdOrThrow(HttpContext);

            // Get the todo first
            var currentTodo = await _todoService.GetTodoByIdAsync(userId, id);

            // Update with IsCompleted = true
            var updateRequest = new UpdateTodoRequest
            {
                Title = currentTodo.Title,
                Description = currentTodo.Description,
                DueDate = currentTodo.DueDate,
                Priority = currentTodo.Priority,
                IsCompleted = true,
                Category = currentTodo.Category,
                Tags = currentTodo.Tags
            };

            var todo = await _todoService.UpdateTodoAsync(userId, id, updateRequest);
            return Ok(todo);
        }
        catch (TodoNotFoundException ex)
        {
            _logger.LogWarning("Todo not found: {Message}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized: {Message}", ex.Message);
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing todo");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPatch("{id}/uncomplete")]
    [ProducesResponseType(typeof(TodoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UncompleteTodo(long id)
    {
        try
        {
            _logger.LogInformation("Uncompleting todo: {Id}", id);

            var userId = AuthHelper.GetUserIdOrThrow(HttpContext);

            // Get the todo first
            var currentTodo = await _todoService.GetTodoByIdAsync(userId, id);

            // Update with IsCompleted = false
            var updateRequest = new UpdateTodoRequest
            {
                Title = currentTodo.Title,
                Description = currentTodo.Description,
                DueDate = currentTodo.DueDate,
                Priority = currentTodo.Priority,
                IsCompleted = false,
                Category = currentTodo.Category,
                Tags = currentTodo.Tags
            };

            var todo = await _todoService.UpdateTodoAsync(userId, id, updateRequest);
            return Ok(todo);
        }
        catch (TodoNotFoundException ex)
        {
            _logger.LogWarning("Todo not found: {Message}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized: {Message}", ex.Message);
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uncompleting todo");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
