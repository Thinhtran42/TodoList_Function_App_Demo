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

public class GetTodoByIdFunction
{
    private readonly MyDbContext _db;
    public GetTodoByIdFunction(MyDbContext db) => _db = db;

    [OpenApiOperation(operationId: "GetTodoById", tags: new[] { "Todos" }, Summary = "Get a todo by id")]
    [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(long), Summary = "Todo id")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(object), Summary = "Found")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(object), Summary = "Not found")]
    [Function("GetTodoById")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "todos/{id:long}")]
        HttpRequestData req,
        long id)
    {
        var entity = await _db.TodoItems
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity is null)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteAsJsonAsync(new { error = $"Todo {id} not found" });
            return notFound;
        }

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
