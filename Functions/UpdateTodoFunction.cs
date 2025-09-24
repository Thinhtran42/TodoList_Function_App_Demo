using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ToDoFunction.Contracts;
using ToDoFunction.Infrastructure;

namespace ToDoFunction.Functions;

public class UpdateTodoFunction
{
    private readonly MyDbContext _db;
    public UpdateTodoFunction(MyDbContext db) => _db = db;

    [OpenApiOperation(operationId: "UpdateTodo", tags: new[] { "Todos" }, Summary = "Update a todo")]
    [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(long), Summary = "Todo id")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(UpdateTodoRequest), Required = true, Description = "Title and/or IsCompleted")]
    [OpenApiResponseWithBody(statusCode: System.Net.HttpStatusCode.OK,
    contentType: "application/json",
    bodyType: typeof(TodoItem),
    Summary = "Updated")]
    [OpenApiResponseWithBody(statusCode: System.Net.HttpStatusCode.BadRequest,
    contentType: "application/json",
    bodyType: typeof(object),
    Summary = "Invalid body")]
    [OpenApiResponseWithBody(statusCode: System.Net.HttpStatusCode.NotFound,
    contentType: "application/json",
    bodyType: typeof(object),
    Summary = "Not found")]
    [Function("UpdateTodo")]
    public async Task<HttpResponseData> Run(
         [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "todos/{id:long}")]
        HttpRequestData req,
         long id)
    {
        var body = await req.ReadFromJsonAsync<UpdateTodoRequest>();
        if (body is null)
        {
            var resp = req.CreateResponse(HttpStatusCode.BadRequest);
            await resp.WriteAsJsonAsync(new { error = "Invalid body" });
            return resp;
        }

        var entity = await _db.TodoItems.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
        {
            var resp = req.CreateResponse(HttpStatusCode.NotFound);
            await resp.WriteAsJsonAsync(new { error = $"Todo {id} not found" });
            return resp;
        }

        if (!string.IsNullOrWhiteSpace(body.Title))
            entity.Title = body.Title;

        if (body.IsCompleted.HasValue)
            entity.IsCompleted = body.IsCompleted.Value;

        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        var ok = req.CreateResponse(HttpStatusCode.OK);
        await ok.WriteAsJsonAsync(new
        {
            entity.Id,
            entity.Title,
            entity.IsCompleted,
            entity.CreatedAt,
            entity.UpdatedAt
        });
        return ok;
    }
}
