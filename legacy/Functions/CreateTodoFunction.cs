using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ToDoFunction.Contracts;
using ToDoFunction.Infrastructure;

namespace ToDoFunction.Functions;
public class CreateTodoFunction
{
    private readonly MyDbContext _db;
    public CreateTodoFunction(MyDbContext db) => _db = db;

    [OpenApiOperation(operationId: "CreateTodo", tags: new[] { "Todos" }, Summary = "Create a new todo")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(CreateTodoRequest), Required = true, Description = "Title only")]
    [OpenApiResponseWithBody(statusCode: System.Net.HttpStatusCode.Created,
    contentType: "application/json",
    bodyType: typeof(TodoItem),
    Summary = "Created")]
    [OpenApiResponseWithBody(statusCode: System.Net.HttpStatusCode.BadRequest,
    contentType: "application/json",
    bodyType: typeof(object),
    Summary = "Invalid body")]
    [Function("CreateTodo")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "todos")] HttpRequestData req)
    {
        var body = await req.ReadFromJsonAsync<CreateTodoRequest>();
        if (body == null || string.IsNullOrWhiteSpace(body.Title))
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("Title is required");
            return bad;
        }

        var entity = new TodoItem
        {
            Title = body.Title,
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.TodoItems.Add(entity);
        await _db.SaveChangesAsync();

        var res = req.CreateResponse(HttpStatusCode.Created);
        await res.WriteAsJsonAsync(new
        {
            entity.Id,
            entity.Title,
            entity.IsCompleted,
            entity.CreatedAt,
            entity.UpdatedAt
        });

        return res;
    }
}

