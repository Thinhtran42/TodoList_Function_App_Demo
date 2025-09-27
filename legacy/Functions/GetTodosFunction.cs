using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ToDoFunction.Infrastructure;

namespace ToDoFunction.Functions;
public class GetTodosFunction
{
    private readonly MyDbContext _db;
    public GetTodosFunction(MyDbContext db) => _db = db;

    [OpenApiOperation(operationId: "GetTodos", tags: new[] { "Todos" }, Summary = "List all todos")]
    [OpenApiResponseWithBody(statusCode: System.Net.HttpStatusCode.OK,
    contentType: "application/json",
    bodyType: typeof(IEnumerable<TodoItem>),
    Summary = "OK")]
    [Function("GetTodos")]
    public async Task<HttpResponseData> Run(
       [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "todos")]
        HttpRequestData req)
    {
        var items = await _db.TodoItems
            .AsNoTracking()
            .OrderByDescending(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.Title,
                x.IsCompleted,
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync();

        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(items);
        return res;
    }
}

