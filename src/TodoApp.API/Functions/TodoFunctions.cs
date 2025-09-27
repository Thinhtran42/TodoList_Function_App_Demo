using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using System.Net;
using TodoApp.Application.DTOs;
using TodoApp.Application.UseCases;
using TodoApp.Domain.Exceptions;

namespace TodoApp.API.Functions;

public class TodoFunctions
{
    private readonly ILogger<TodoFunctions> _logger;
    private readonly CreateTodoUseCase _createTodoUseCase;
    private readonly GetTodoByIdUseCase _getTodoByIdUseCase;
    private readonly GetAllTodosUseCase _getAllTodosUseCase;
    private readonly UpdateTodoUseCase _updateTodoUseCase;
    private readonly DeleteTodoUseCase _deleteTodoUseCase;

    public TodoFunctions(
        ILogger<TodoFunctions> logger,
        CreateTodoUseCase createTodoUseCase,
        GetTodoByIdUseCase getTodoByIdUseCase,
        GetAllTodosUseCase getAllTodosUseCase,
        UpdateTodoUseCase updateTodoUseCase,
        DeleteTodoUseCase deleteTodoUseCase)
    {
        _logger = logger;
        _createTodoUseCase = createTodoUseCase;
        _getTodoByIdUseCase = getTodoByIdUseCase;
        _getAllTodosUseCase = getAllTodosUseCase;
        _updateTodoUseCase = updateTodoUseCase;
        _deleteTodoUseCase = deleteTodoUseCase;
    }

    [Function("CreateTodo")]
    [OpenApiOperation(operationId: "CreateTodo", tags: new[] { "Todos" }, Summary = "Create a new todo")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(CreateTodoRequest), Required = true, Description = "Todo creation request")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(TodoDto), Summary = "Todo created")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(object), Summary = "Invalid request")]
    public async Task<HttpResponseData> CreateTodo(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "todos")] HttpRequestData req)
    {
        try
        {
            _logger.LogInformation("Creating new todo");

            var request = await req.ReadFromJsonAsync<CreateTodoRequest>();
            if (request == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "Invalid request body" });
                return badResponse;
            }

            var todo = await _createTodoUseCase.ExecuteAsync(request);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(todo);
            return response;
        }
        catch (TodoValidationException ex)
        {
            _logger.LogWarning("Validation error: {Message}", ex.Message);
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(new { error = ex.Message });
            return badResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating todo");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = "Internal server error" });
            return errorResponse;
        }
    }

    [Function("GetTodos")]
    [OpenApiOperation(operationId: "GetTodos", tags: new[] { "Todos" }, Summary = "Get all todos")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(IEnumerable<TodoDto>), Summary = "List of todos")]
    public async Task<HttpResponseData> GetTodos(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "todos")] HttpRequestData req)
    {
        try
        {
            _logger.LogInformation("Getting all todos");

            var todos = await _getAllTodosUseCase.ExecuteAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(todos);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting todos");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = "Internal server error" });
            return errorResponse;
        }
    }

    [Function("GetTodoById")]
    [OpenApiOperation(operationId: "GetTodoById", tags: new[] { "Todos" }, Summary = "Get a todo by ID")]
    [OpenApiParameter(name: "id", In = Microsoft.OpenApi.Models.ParameterLocation.Path, Required = true, Type = typeof(long), Summary = "Todo ID")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(TodoDto), Summary = "Todo found")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(object), Summary = "Todo not found")]
    public async Task<HttpResponseData> GetTodoById(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "todos/{id:long}")] HttpRequestData req,
        long id)
    {
        try
        {
            _logger.LogInformation("Getting todo by ID: {TodoId}", id);

            var todo = await _getTodoByIdUseCase.ExecuteAsync(id);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(todo);
            return response;
        }
        catch (TodoNotFoundException ex)
        {
            _logger.LogWarning("Todo not found: {TodoId}", ex.TodoId);
            var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
            await notFoundResponse.WriteAsJsonAsync(new { error = ex.Message });
            return notFoundResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting todo by ID: {TodoId}", id);
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = "Internal server error" });
            return errorResponse;
        }
    }

    [Function("UpdateTodo")]
    [OpenApiOperation(operationId: "UpdateTodo", tags: new[] { "Todos" }, Summary = "Update a todo")]
    [OpenApiParameter(name: "id", In = Microsoft.OpenApi.Models.ParameterLocation.Path, Required = true, Type = typeof(long), Summary = "Todo ID")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(UpdateTodoRequest), Required = true, Description = "Todo update request")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(TodoDto), Summary = "Todo updated")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(object), Summary = "Todo not found")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(object), Summary = "Invalid request")]
    public async Task<HttpResponseData> UpdateTodo(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "todos/{id:long}")] HttpRequestData req,
        long id)
    {
        try
        {
            _logger.LogInformation("Updating todo with ID: {TodoId}", id);

            var request = await req.ReadFromJsonAsync<UpdateTodoRequest>();
            if (request == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "Invalid request body" });
                return badResponse;
            }

            var todo = await _updateTodoUseCase.ExecuteAsync(id, request);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(todo);
            return response;
        }
        catch (TodoNotFoundException ex)
        {
            _logger.LogWarning("Todo not found: {TodoId}", ex.TodoId);
            var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
            await notFoundResponse.WriteAsJsonAsync(new { error = ex.Message });
            return notFoundResponse;
        }
        catch (TodoValidationException ex)
        {
            _logger.LogWarning("Validation error: {Message}", ex.Message);
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(new { error = ex.Message });
            return badResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating todo with ID: {TodoId}", id);
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = "Internal server error" });
            return errorResponse;
        }
    }

    [Function("DeleteTodo")]
    [OpenApiOperation(operationId: "DeleteTodo", tags: new[] { "Todos" }, Summary = "Delete a todo")]
    [OpenApiParameter(name: "id", In = Microsoft.OpenApi.Models.ParameterLocation.Path, Required = true, Type = typeof(long), Summary = "Todo ID")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NoContent, Summary = "Todo deleted")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(object), Summary = "Todo not found")]
    public async Task<HttpResponseData> DeleteTodo(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "todos/{id:long}")] HttpRequestData req,
        long id)
    {
        try
        {
            _logger.LogInformation("Deleting todo with ID: {TodoId}", id);

            await _deleteTodoUseCase.ExecuteAsync(id);

            return req.CreateResponse(HttpStatusCode.NoContent);
        }
        catch (TodoNotFoundException ex)
        {
            _logger.LogWarning("Todo not found: {TodoId}", ex.TodoId);
            var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
            await notFoundResponse.WriteAsJsonAsync(new { error = ex.Message });
            return notFoundResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting todo with ID: {TodoId}", id);
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = "Internal server error" });
            return errorResponse;
        }
    }
}