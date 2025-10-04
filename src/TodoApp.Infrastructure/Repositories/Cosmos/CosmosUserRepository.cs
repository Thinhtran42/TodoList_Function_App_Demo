using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using TodoApp.Application.Interfaces;
using TodoApp.Infrastructure.Data.Models;
using DomainUser = TodoApp.Domain.Entities.User;
using DomainRefreshToken = TodoApp.Domain.Entities.RefreshToken;

namespace TodoApp.Infrastructure.Repositories.Cosmos;

public class CosmosUserRepository : IUserRepository
{
    private readonly CosmosClient _cosmosClient;
    private readonly Container _usersContainer;
    private readonly Container _refreshTokensContainer;

    public CosmosUserRepository(CosmosClient cosmosClient, IConfiguration configuration)
    {
        _cosmosClient = cosmosClient;
        var databaseName = configuration["CosmosDB:DatabaseName"] ?? "TodoApp";
        
        var database = _cosmosClient.GetDatabase(databaseName);
        _usersContainer = database.GetContainer("Users");
        _refreshTokensContainer = database.GetContainer("RefreshTokens");
    }

    public async Task<DomainUser?> GetByIdAsync(long id)
    {
        try
        {
            var response = await _usersContainer.ReadItemAsync<CosmosUser>(
                id.ToString(), 
                new PartitionKey(id));
                
            return response.Resource?.ToDomain();
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IEnumerable<DomainUser>> GetAllAsync()
    {
        var query = new QueryDefinition("SELECT * FROM c ORDER BY c.DomainId DESC");
        var iterator = _usersContainer.GetItemQueryIterator<CosmosUser>(query);
        
        var users = new List<DomainUser>();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            users.AddRange(response.Select(u => u.ToDomain()));
        }
        
        return users;
    }

    public async Task<DomainUser> CreateAsync(DomainUser entity)
    {
        var cosmosUser = CosmosUser.FromDomain(entity);
        cosmosUser.id = Guid.NewGuid().ToString();
        cosmosUser.createdAt = DateTime.UtcNow;
        cosmosUser.updatedAt = DateTime.UtcNow;
        
        var response = await _usersContainer.CreateItemAsync(
            cosmosUser, 
            new PartitionKey(cosmosUser.DomainId));
            
        return response.Resource.ToDomain();
    }

    public async Task<DomainUser?> UpdateAsync(DomainUser entity)
    {
        try
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.DomainId = @domainId")
                .WithParameter("@domainId", entity.Id);
                
            var iterator = _usersContainer.GetItemQueryIterator<CosmosUser>(query);
            var response = await iterator.ReadNextAsync();
            var existingUser = response.FirstOrDefault();
            
            if (existingUser == null)
                return null;

            // Update properties
            existingUser.username = entity.Username;
            existingUser.email = entity.Email;
            existingUser.passwordHash = entity.PasswordHash;
            existingUser.firstName = entity.FirstName;
            existingUser.lastName = entity.LastName;
            existingUser.updatedAt = DateTime.UtcNow;

            var updateResponse = await _usersContainer.ReplaceItemAsync(
                existingUser, 
                existingUser.id,
                new PartitionKey(existingUser.DomainId));
                
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
            await _usersContainer.DeleteItemAsync<CosmosUser>(
                id.ToString(), 
                new PartitionKey(id));
            return true;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<bool> ExistsAsync(long id)
    {
        try
        {
            await _usersContainer.ReadItemAsync<CosmosUser>(
                id.ToString(), 
                new PartitionKey(id));
            return true;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<DomainUser?> GetByUsernameAsync(string username)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.username = @username")
            .WithParameter("@username", username);
            
        var iterator = _usersContainer.GetItemQueryIterator<CosmosUser>(query);
        var response = await iterator.ReadNextAsync();
        
        return response.FirstOrDefault()?.ToDomain();
    }

    public async Task<DomainUser?> GetByEmailAsync(string email)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.email = @email")
            .WithParameter("@email", email);
            
        var iterator = _usersContainer.GetItemQueryIterator<CosmosUser>(query);
        var response = await iterator.ReadNextAsync();
        
        return response.FirstOrDefault()?.ToDomain();
    }

    public async Task<bool> UserExistsByUsernameAsync(string username)
    {
        var query = new QueryDefinition("SELECT VALUE COUNT(1) FROM c WHERE c.username = @username")
            .WithParameter("@username", username);
            
        var iterator = _usersContainer.GetItemQueryIterator<int>(query);
        var response = await iterator.ReadNextAsync();
        
        return response.FirstOrDefault() > 0;
    }

    public async Task<bool> UserExistsByEmailAsync(string email)
    {
        var query = new QueryDefinition("SELECT VALUE COUNT(1) FROM c WHERE c.email = @email")
            .WithParameter("@email", email);
            
        var iterator = _usersContainer.GetItemQueryIterator<int>(query);
        var response = await iterator.ReadNextAsync();
        
        return response.FirstOrDefault() > 0;
    }

    public async Task<DomainRefreshToken?> GetRefreshTokenAsync(string token)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.token = @token")
            .WithParameter("@token", token);
            
        var iterator = _refreshTokensContainer.GetItemQueryIterator<CosmosRefreshToken>(query);
        var response = await iterator.ReadNextAsync();
        
        return response.FirstOrDefault()?.ToDomain();
    }

    public async Task AddRefreshTokenAsync(DomainRefreshToken refreshToken)
    {
        var cosmosRefreshToken = CosmosRefreshToken.FromDomain(refreshToken);
        cosmosRefreshToken.id = Guid.NewGuid().ToString();
        cosmosRefreshToken.createdAt = DateTime.UtcNow;
        cosmosRefreshToken.updatedAt = DateTime.UtcNow;
        
        await _refreshTokensContainer.CreateItemAsync(
            cosmosRefreshToken, 
            new PartitionKey(cosmosRefreshToken.userId));
    }

    public async Task RemoveRefreshTokenAsync(string token)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.token = @token")
            .WithParameter("@token", token);
            
        var iterator = _refreshTokensContainer.GetItemQueryIterator<CosmosRefreshToken>(query);
        var response = await iterator.ReadNextAsync();
        var refreshToken = response.FirstOrDefault();
        
        if (refreshToken != null)
        {
            await _refreshTokensContainer.DeleteItemAsync<CosmosRefreshToken>(
                refreshToken.id,
                new PartitionKey(refreshToken.userId));
        }
    }

    public async Task RemoveExpiredRefreshTokensAsync(long userId)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.userId = @userId AND c.expiresAt < @now")
            .WithParameter("@userId", userId)
            .WithParameter("@now", DateTime.UtcNow);
            
        var iterator = _refreshTokensContainer.GetItemQueryIterator<CosmosRefreshToken>(query);
        
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            foreach (var token in response)
            {
                await _refreshTokensContainer.DeleteItemAsync<CosmosRefreshToken>(
                    token.id,
                    new PartitionKey(token.userId));
            }
        }
    }

    public async Task<bool> ExistsAsync(string username, string email)
    {
        var query = new QueryDefinition("SELECT VALUE COUNT(1) FROM c WHERE c.username = @username OR c.email = @email")
            .WithParameter("@username", username)
            .WithParameter("@email", email);
            
        var iterator = _usersContainer.GetItemQueryIterator<int>(query);
        var response = await iterator.ReadNextAsync();
        
        return response.FirstOrDefault() > 0;
    }

    public async Task<DomainUser?> GetByIdWithTodosAsync(long id)
    {
        // In Cosmos DB with separate containers, we don't embed TodoItems
        return await GetByIdAsync(id);
    }
}