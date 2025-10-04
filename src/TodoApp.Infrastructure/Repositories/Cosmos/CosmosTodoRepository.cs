using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using TodoApp.Application.DTOs;
using TodoApp.Application.Common;
using TodoApp.Application.Interfaces;
using TodoApp.Infrastructure.Data.Models;
using DomainTodoItem = TodoApp.Domain.Entities.TodoItem;

namespace TodoApp.Infrastructure.Repositories.Cosmos;

public class CosmosTodoRepository : ITodoRepository
{
    private readonly CosmosClient _cosmosClient;
    private readonly Container _todoItemsContainer;

    public CosmosTodoRepository(CosmosClient cosmosClient, IConfiguration configuration)
    {
        _cosmosClient = cosmosClient;
        var databaseName = configuration["CosmosDB:DatabaseName"] ?? "TodoApp";
        
        var database = _cosmosClient.GetDatabase(databaseName);
        _todoItemsContainer = database.GetContainer("TodoItems");
    }

    public async Task<DomainTodoItem?> GetByIdAsync(long id)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.DomainId = @domainId")
            .WithParameter("@domainId", id);
            
        var iterator = _todoItemsContainer.GetItemQueryIterator<CosmosTodoItem>(query);
        var response = await iterator.ReadNextAsync();
        
        return response.FirstOrDefault()?.ToDomain();
    }

    public async Task<IEnumerable<DomainTodoItem>> GetAllAsync()
    {
        var query = new QueryDefinition("SELECT * FROM c ORDER BY c.DomainId DESC");
        var iterator = _todoItemsContainer.GetItemQueryIterator<CosmosTodoItem>(query);
        
        var todoItems = new List<DomainTodoItem>();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            todoItems.AddRange(response.Select(t => t.ToDomain()));
        }
        
        return todoItems;
    }

    public async Task<DomainTodoItem> CreateAsync(DomainTodoItem entity)
    {
        var cosmosTodoItem = CosmosTodoItem.FromDomain(entity);
        cosmosTodoItem.id = Guid.NewGuid().ToString();
        cosmosTodoItem.createdAt = DateTime.UtcNow;
        cosmosTodoItem.updatedAt = DateTime.UtcNow;
        
        var response = await _todoItemsContainer.CreateItemAsync(
            cosmosTodoItem, 
            new PartitionKey(cosmosTodoItem.userId));
            
        return response.Resource.ToDomain();
    }

    public async Task<DomainTodoItem?> UpdateAsync(DomainTodoItem entity)
    {
        try
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.DomainId = @domainId AND c.userId = @userId")
                .WithParameter("@domainId", entity.Id)
                .WithParameter("@userId", entity.UserId);
                
            var iterator = _todoItemsContainer.GetItemQueryIterator<CosmosTodoItem>(query);
            var response = await iterator.ReadNextAsync();
            var existingItem = response.FirstOrDefault();
            
            if (existingItem == null)
                return null;

            // Update properties
            existingItem.title = entity.Title;
            existingItem.description = entity.Description;
            existingItem.isCompleted = entity.IsCompleted;
            existingItem.priority = (int)entity.Priority;
            existingItem.category = (int)entity.Category;
            existingItem.dueDate = entity.DueDate;
            existingItem.tags = entity.Tags;
            existingItem.updatedAt = DateTime.UtcNow;

            var updateResponse = await _todoItemsContainer.ReplaceItemAsync(
                existingItem, 
                existingItem.id,
                new PartitionKey(existingItem.userId));
                
            return updateResponse.Resource.ToDomain();
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<bool> DeleteAsync(long id)
    {
        try
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.DomainId = @domainId")
                .WithParameter("@domainId", id);
                
            var iterator = _todoItemsContainer.GetItemQueryIterator<CosmosTodoItem>(query);
            var response = await iterator.ReadNextAsync();
            var item = response.FirstOrDefault();
            
            if (item == null)
                return false;

            await _todoItemsContainer.DeleteItemAsync<CosmosTodoItem>(
                item.id,
                new PartitionKey(item.userId));
            return true;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<bool> ExistsAsync(long id)
    {
        var query = new QueryDefinition("SELECT VALUE COUNT(1) FROM c WHERE c.DomainId = @domainId")
            .WithParameter("@domainId", id);
            
        var iterator = _todoItemsContainer.GetItemQueryIterator<int>(query);
        var response = await iterator.ReadNextAsync();
        
        return response.FirstOrDefault() > 0;
    }

    public async Task<DomainTodoItem?> GetByIdAsync(long userId, long id)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.userId = @userId AND c.DomainId = @domainId")
            .WithParameter("@userId", userId)
            .WithParameter("@domainId", id);
            
        var iterator = _todoItemsContainer.GetItemQueryIterator<CosmosTodoItem>(query);
        var response = await iterator.ReadNextAsync();
        
        return response.FirstOrDefault()?.ToDomain();
    }

    public async Task<IEnumerable<DomainTodoItem>> GetAllAsync(long userId)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.userId = @userId ORDER BY c.DomainId DESC")
            .WithParameter("@userId", userId);
            
        var iterator = _todoItemsContainer.GetItemQueryIterator<CosmosTodoItem>(query);
        
        var todoItems = new List<DomainTodoItem>();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            todoItems.AddRange(response.Select(t => t.ToDomain()));
        }
        
        return todoItems;
    }

    public async Task<bool> ExistsAsync(long userId, long id)
    {
        var query = new QueryDefinition("SELECT VALUE COUNT(1) FROM c WHERE c.userId = @userId AND c.DomainId = @domainId")
            .WithParameter("@userId", userId)
            .WithParameter("@domainId", id);
            
        var iterator = _todoItemsContainer.GetItemQueryIterator<int>(query);
        var response = await iterator.ReadNextAsync();
        
        return response.FirstOrDefault() > 0;
    }

    public async Task<IEnumerable<DomainTodoItem>> GetCompletedTodosAsync(long userId)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.userId = @userId AND c.isCompleted = true ORDER BY c.DomainId DESC")
            .WithParameter("@userId", userId);
            
        var iterator = _todoItemsContainer.GetItemQueryIterator<CosmosTodoItem>(query);
        
        var todoItems = new List<DomainTodoItem>();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            todoItems.AddRange(response.Select(t => t.ToDomain()));
        }
        
        return todoItems;
    }

    public async Task<IEnumerable<DomainTodoItem>> GetIncompleteTodosAsync(long userId)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.userId = @userId AND c.isCompleted = false ORDER BY c.DomainId DESC")
            .WithParameter("@userId", userId);
            
        var iterator = _todoItemsContainer.GetItemQueryIterator<CosmosTodoItem>(query);
        
        var todoItems = new List<DomainTodoItem>();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            todoItems.AddRange(response.Select(t => t.ToDomain()));
        }
        
        return todoItems;
    }

    public async Task<PagedResult<DomainTodoItem>> GetTodosAsync(long userId, TodoQueryParameters parameters)
    {
        var conditions = new List<string> { "c.userId = @userId" };
        var queryParams = new Dictionary<string, object> { { "@userId", userId } };

        // Apply filters
        if (parameters.IsCompleted.HasValue)
        {
            conditions.Add("c.isCompleted = @isCompleted");
            queryParams.Add("@isCompleted", parameters.IsCompleted.Value);
        }

        if (parameters.Priority.HasValue)
        {
            conditions.Add("c.priority = @priority");
            queryParams.Add("@priority", (int)parameters.Priority.Value);
        }

        if (parameters.Category.HasValue)
        {
            conditions.Add("c.category = @category");
            queryParams.Add("@category", (int)parameters.Category.Value);
        }

        if (parameters.DueDateFrom.HasValue)
        {
            conditions.Add("c.dueDate >= @dueDateFrom");
            queryParams.Add("@dueDateFrom", parameters.DueDateFrom.Value);
        }

        if (parameters.DueDateTo.HasValue)
        {
            conditions.Add("c.dueDate <= @dueDateTo");
            queryParams.Add("@dueDateTo", parameters.DueDateTo.Value);
        }

        if (!string.IsNullOrEmpty(parameters.SearchTerm))
        {
            conditions.Add("(CONTAINS(LOWER(c.title), @searchTerm) OR CONTAINS(LOWER(c.description), @searchTerm))");
            queryParams.Add("@searchTerm", parameters.SearchTerm.ToLower());
        }

        if (!string.IsNullOrEmpty(parameters.Tags))
        {
            conditions.Add("CONTAINS(LOWER(c.tags), @tags)");
            queryParams.Add("@tags", parameters.Tags.ToLower());
        }

        var whereClause = string.Join(" AND ", conditions);
        
        // Count query
        var countSql = $"SELECT VALUE COUNT(1) FROM c WHERE {whereClause}";
        var countQuery = new QueryDefinition(countSql);
        foreach (var param in queryParams)
        {
            countQuery = countQuery.WithParameter(param.Key, param.Value);
        }
        
        var countIterator = _todoItemsContainer.GetItemQueryIterator<int>(countQuery);
        var countResponse = await countIterator.ReadNextAsync();
        var totalItems = countResponse.FirstOrDefault();

        // Data query with sorting and pagination
        var sortBy = parameters.SortBy ?? "createdAt";
        var sortDirection = parameters.SortDescending ? "DESC" : "ASC";
        var skip = (parameters.Page - 1) * parameters.PageSize;
        
        var dataSql = $"SELECT * FROM c WHERE {whereClause} ORDER BY c.{sortBy} {sortDirection} OFFSET {skip} LIMIT {parameters.PageSize}";
        var dataQuery = new QueryDefinition(dataSql);
        foreach (var param in queryParams)
        {
            dataQuery = dataQuery.WithParameter(param.Key, param.Value);
        }
        
        var dataIterator = _todoItemsContainer.GetItemQueryIterator<CosmosTodoItem>(dataQuery);
        
        var todoItems = new List<DomainTodoItem>();
        while (dataIterator.HasMoreResults)
        {
            var response = await dataIterator.ReadNextAsync();
            todoItems.AddRange(response.Select(t => t.ToDomain()));
        }

        return PagedResult<DomainTodoItem>.Create(
            todoItems,
            totalItems,
            parameters.Page,
            parameters.PageSize
        );
    }

    public async Task<IEnumerable<DomainTodoItem>> SearchTodosAsync(long userId, string searchTerm)
    {
        if (string.IsNullOrEmpty(searchTerm))
        {
            return new List<DomainTodoItem>();
        }

        var query = new QueryDefinition("SELECT * FROM c WHERE c.userId = @userId AND (CONTAINS(LOWER(c.title), @searchTerm) OR CONTAINS(LOWER(c.description), @searchTerm) OR CONTAINS(LOWER(c.tags), @searchTerm)) ORDER BY c.createdAt DESC")
            .WithParameter("@userId", userId)
            .WithParameter("@searchTerm", searchTerm.ToLower());
            
        var iterator = _todoItemsContainer.GetItemQueryIterator<CosmosTodoItem>(query);
        
        var todoItems = new List<DomainTodoItem>();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            todoItems.AddRange(response.Select(t => t.ToDomain()));
        }
        
        return todoItems;
    }

    public async Task<IEnumerable<DomainTodoItem>> GetTodosByTagsAsync(long userId, string tags)
    {
        if (string.IsNullOrEmpty(tags))
        {
            return new List<DomainTodoItem>();
        }

        var query = new QueryDefinition("SELECT * FROM c WHERE c.userId = @userId AND CONTAINS(LOWER(c.tags), @tags) ORDER BY c.createdAt DESC")
            .WithParameter("@userId", userId)
            .WithParameter("@tags", tags.ToLower());
            
        var iterator = _todoItemsContainer.GetItemQueryIterator<CosmosTodoItem>(query);
        
        var todoItems = new List<DomainTodoItem>();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            todoItems.AddRange(response.Select(t => t.ToDomain()));
        }
        
        return todoItems;
    }

    public async Task<IEnumerable<DomainTodoItem>> GetOverdueTodosAsync(long userId)
    {
        var now = DateTime.UtcNow;
        var query = new QueryDefinition("SELECT * FROM c WHERE c.userId = @userId AND c.isCompleted = false AND c.dueDate < @now ORDER BY c.dueDate")
            .WithParameter("@userId", userId)
            .WithParameter("@now", now);
            
        var iterator = _todoItemsContainer.GetItemQueryIterator<CosmosTodoItem>(query);
        
        var todoItems = new List<DomainTodoItem>();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            todoItems.AddRange(response.Select(t => t.ToDomain()));
        }
        
        return todoItems;
    }
}