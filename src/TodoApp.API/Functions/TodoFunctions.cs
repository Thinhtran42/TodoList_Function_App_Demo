using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Web;
using TodoApp.API.Helpers;
using TodoApp.Application.DTOs;
using TodoApp.Application.Common;
using TodoApp.Application.Interfaces;
using TodoApp.Domain.Entities;
using TodoApp.Domain.Exceptions;

namespace TodoApp.API.Functions;

public class TodoFunctions
{
    private readonly ILogger<TodoFunctions> _logger;
    private readonly ITodoService _todoService;
    private readonly IValidator<CreateTodoRequest> _createTodoValidator;
    private readonly IValidator<UpdateTodoRequest> _updateTodoValidator;
    private readonly IValidator<TodoQueryParameters> _queryParametersValidator;

    public TodoFunctions(
        ILogger<TodoFunctions> logger,
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

            // Validate request
            var (isValid, validationResponse) = await ValidationHelper.ValidateAsync(_createTodoValidator, request, req);
            if (!isValid && validationResponse != null)
            {
                return validationResponse;
            }

            var todo = await _todoService.CreateTodoAsync(request);

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
    [OpenApiOperation(operationId: "GetTodos", tags: new[] { "Todos" }, Summary = "Get todos with filtering, sorting and pagination")]
    [OpenApiParameter(name: "isCompleted", In = Microsoft.OpenApi.Models.ParameterLocation.Query, Type = typeof(bool?), Summary = "Filter by completion status")]
    [OpenApiParameter(name: "priority", In = Microsoft.OpenApi.Models.ParameterLocation.Query, Type = typeof(int?), Summary = "Filter by priority (1=Low, 2=Medium, 3=High, 4=Critical)")]
    [OpenApiParameter(name: "category", In = Microsoft.OpenApi.Models.ParameterLocation.Query, Type = typeof(int?), Summary = "Filter by category")]
    [OpenApiParameter(name: "dueDateFrom", In = Microsoft.OpenApi.Models.ParameterLocation.Query, Type = typeof(DateTime?), Summary = "Filter by due date from")]
    [OpenApiParameter(name: "dueDateTo", In = Microsoft.OpenApi.Models.ParameterLocation.Query, Type = typeof(DateTime?), Summary = "Filter by due date to")]
    [OpenApiParameter(name: "searchTerm", In = Microsoft.OpenApi.Models.ParameterLocation.Query, Type = typeof(string), Summary = "Search in title and description")]
    [OpenApiParameter(name: "tags", In = Microsoft.OpenApi.Models.ParameterLocation.Query, Type = typeof(string), Summary = "Filter by tags")]
    [OpenApiParameter(name: "sortBy", In = Microsoft.OpenApi.Models.ParameterLocation.Query, Type = typeof(string), Summary = "Sort field (default: CreatedAt)")]
    [OpenApiParameter(name: "sortDescending", In = Microsoft.OpenApi.Models.ParameterLocation.Query, Type = typeof(bool), Summary = "Sort descending (default: true)")]
    [OpenApiParameter(name: "page", In = Microsoft.OpenApi.Models.ParameterLocation.Query, Type = typeof(int), Summary = "Page number (default: 1)")]
    [OpenApiParameter(name: "pageSize", In = Microsoft.OpenApi.Models.ParameterLocation.Query, Type = typeof(int), Summary = "Page size (default: 10, max: 100)")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(PagedResult<TodoDto>), Summary = "Paginated list of todos")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(object), Summary = "Invalid query parameters")]
    public async Task<HttpResponseData> GetTodos(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "todos")] HttpRequestData req)
    {
        try
        {
            _logger.LogInformation("Getting todos with query parameters");

            var queryParams = ExtractQueryParameters(req);
            
            // Validate query parameters
            var (isValid, validationResponse) = await ValidationHelper.ValidateAsync(_queryParametersValidator, queryParams, req);
            if (!isValid && validationResponse != null)
            {
                return validationResponse;
            }

            var todos = await _todoService.GetTodosAsync(queryParams);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(todos);
            return response;
        }
        catch (TodoValidationException ex)
        {
            _logger.LogWarning("Validation error in query parameters: {Message}", ex.Message);
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(new { error = ex.Message });
            return badResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting todos");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = "Internal server error" });
            return errorResponse;
        }
    }

    private TodoQueryParameters ExtractQueryParameters(HttpRequestData req)
    {
        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);

        var parameters = new TodoQueryParameters();

        if (bool.TryParse(query["isCompleted"], out var isCompleted))
            parameters.IsCompleted = isCompleted;

        if (Enum.TryParse<Priority>(query["priority"], out var priority))
            parameters.Priority = priority;

        if (Enum.TryParse<Category>(query["category"], out var category))
            parameters.Category = category;

        if (DateTime.TryParse(query["dueDateFrom"], out var dueDateFrom))
            parameters.DueDateFrom = dueDateFrom;

        if (DateTime.TryParse(query["dueDateTo"], out var dueDateTo))
            parameters.DueDateTo = dueDateTo;

        parameters.SearchTerm = query["searchTerm"];
        parameters.Tags = query["tags"];
        parameters.SortBy = query["sortBy"] ?? "CreatedAt";

        if (bool.TryParse(query["sortDescending"], out var sortDescending))
            parameters.SortDescending = sortDescending;

        if (int.TryParse(query["page"], out var page) && page > 0)
            parameters.Page = page;

        if (int.TryParse(query["pageSize"], out var pageSize) && pageSize > 0 && pageSize <= 100)
            parameters.PageSize = pageSize;

        return parameters;
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

            var todo = await _todoService.GetTodoByIdAsync(id);

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

            // Validate request
            var (isValid, validationResponse) = await ValidationHelper.ValidateAsync(_updateTodoValidator, request, req);
            if (!isValid && validationResponse != null)
            {
                return validationResponse;
            }

                        var todo = await _todoService.UpdateTodoAsync(id, request);

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

            var deleted = await _todoService.DeleteTodoAsync(id);
            
            if (!deleted)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteAsJsonAsync(new { error = $"Todo with ID {id} not found" });
                return notFoundResponse;
            }

            return req.CreateResponse(HttpStatusCode.NoContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting todo with ID: {TodoId}", id);
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = "Internal server error" });
            return errorResponse;
        }
    }

    [Function("SearchTodos")]
    [OpenApiOperation(operationId: "SearchTodos", tags: new[] { "Todos" }, Summary = "Search todos by term")]
    [OpenApiParameter(name: "q", In = Microsoft.OpenApi.Models.ParameterLocation.Query, Required = true, Type = typeof(string), Summary = "Search term")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(IEnumerable<TodoDto>), Summary = "Search results")]
    public async Task<HttpResponseData> SearchTodos(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "todos/search")] HttpRequestData req)
    {
        try
        {
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var searchTerm = query["q"];

            if (string.IsNullOrEmpty(searchTerm))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "Search term is required" });
                return badResponse;
            }

            _logger.LogInformation("Searching todos with term: {SearchTerm}", searchTerm);

            var todos = await _todoService.SearchTodosAsync(searchTerm);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(todos);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching todos");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = "Internal server error" });
            return errorResponse;
        }
    }

    [Function("GetOverdueTodos")]
    [OpenApiOperation(operationId: "GetOverdueTodos", tags: new[] { "Todos" }, Summary = "Get overdue todos")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(IEnumerable<TodoDto>), Summary = "List of overdue todos")]
    public async Task<HttpResponseData> GetOverdueTodos(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "todos/overdue")] HttpRequestData req)
    {
        try
        {
            _logger.LogInformation("Getting overdue todos");

            var todos = await _todoService.GetOverdueTodosAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(todos);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting overdue todos");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = "Internal server error" });
            return errorResponse;
        }
    }
}