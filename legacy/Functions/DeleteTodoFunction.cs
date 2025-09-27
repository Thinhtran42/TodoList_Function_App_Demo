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
using ToDoFunction.Infrastructure;

namespace ToDoFunction.Functions;

public class DeleteTodoFunction
{
    private readonly MyDbContext _db;
    public DeleteTodoFunction(MyDbContext db) => _db = db;

    [OpenApiOperation(operationId: "DeleteTodo", tags: new[] { "Todos" }, Summary = "Delete a todo")]
    [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(long), Summary = "Todo id")]
    [OpenApiResponseWithoutBody(statusCode: System.Net.HttpStatusCode.NoContent, Summary = "Deleted")]
    [OpenApiResponseWithBody(statusCode: System.Net.HttpStatusCode.NotFound,
    contentType: "application/json",
    bodyType: typeof(object),
    Summary = "Not found")]
    [Function("DeleteTodo")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "todos/{id:long}")]
        HttpRequestData req,
        long id)
    {
        var entity = await _db.TodoItems.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteAsJsonAsync(new { error = $"Todo {id} not found" });
            return notFound;
        }

        _db.TodoItems.Remove(entity);
        await _db.SaveChangesAsync();

        // Trả 204 No Content cho chuẩn REST
        var noContent = req.CreateResponse(HttpStatusCode.NoContent);
        return noContent;
    }
}
