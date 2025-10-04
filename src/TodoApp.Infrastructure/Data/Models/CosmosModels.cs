using TodoApp.Domain.Entities;

namespace TodoApp.Infrastructure.Data.Models;

// Cosmos-specific models that map to domain entities
public class CosmosUser
{
    public string id { get; set; } = string.Empty; // Cosmos DB requires string id
    public long DomainId { get; set; } // Keep original long id for mapping
    public string username { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public string passwordHash { get; set; } = string.Empty;
    public string? firstName { get; set; }
    public string? lastName { get; set; }
    public bool isActive { get; set; } = true;
    public DateTime? lastLoginAt { get; set; }
    public DateTime createdAt { get; set; }
    public DateTime updatedAt { get; set; }

    // Convert from domain entity
    public static CosmosUser FromDomain(User user)
    {
        return new CosmosUser
        {
            id = user.Id.ToString(), // Convert long to string
            DomainId = user.Id,
            username = user.Username,
            email = user.Email,
            passwordHash = user.PasswordHash,
            firstName = user.FirstName,
            lastName = user.LastName,
            isActive = user.IsActive,
            lastLoginAt = user.LastLoginAt,
            createdAt = user.CreatedAt,
            updatedAt = user.UpdatedAt
        };
    }

    // Convert to domain entity
    public User ToDomain()
    {
        return new User
        {
            Id = DomainId,
            Username = username,
            Email = email,
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName,
            IsActive = isActive,
            LastLoginAt = lastLoginAt,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }
}

public class CosmosTodoItem
{
    public string id { get; set; } = string.Empty;
    public long DomainId { get; set; }
    public string title { get; set; } = string.Empty;
    public string? description { get; set; }
    public bool isCompleted { get; set; }
    public int priority { get; set; }
    public int category { get; set; }
    public DateTime? dueDate { get; set; }
    public string? tags { get; set; }
    public long userId { get; set; } // Partition key
    public DateTime createdAt { get; set; }
    public DateTime updatedAt { get; set; }

    public static CosmosTodoItem FromDomain(TodoItem todo)
    {
        return new CosmosTodoItem
        {
            id = todo.Id.ToString(),
            DomainId = todo.Id,
            title = todo.Title,
            description = todo.Description,
            isCompleted = todo.IsCompleted,
            priority = (int)todo.Priority,
            category = (int)todo.Category,
            dueDate = todo.DueDate,
            tags = todo.Tags,
            userId = todo.UserId,
            createdAt = todo.CreatedAt,
            updatedAt = todo.UpdatedAt
        };
    }

    public TodoItem ToDomain()
    {
        return new TodoItem
        {
            Id = DomainId,
            Title = title,
            Description = description,
            IsCompleted = isCompleted,
            Priority = (Priority)priority,
            Category = (Category)category,
            DueDate = dueDate,
            Tags = tags,
            UserId = userId,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }
}

public class CosmosRefreshToken
{
    public string id { get; set; } = string.Empty;
    public long DomainId { get; set; }
    public string token { get; set; } = string.Empty;
    public long userId { get; set; } // Partition key
    public DateTime expiresAt { get; set; }
    public bool isRevoked { get; set; }
    public DateTime createdAt { get; set; }
    public DateTime updatedAt { get; set; }

    public static CosmosRefreshToken FromDomain(RefreshToken refreshToken)
    {
        return new CosmosRefreshToken
        {
            id = refreshToken.Id.ToString(),
            DomainId = refreshToken.Id,
            token = refreshToken.Token,
            userId = refreshToken.UserId,
            expiresAt = refreshToken.ExpiresAt,
            isRevoked = refreshToken.IsRevoked,
            createdAt = refreshToken.CreatedAt,
            updatedAt = refreshToken.UpdatedAt
        };
    }

    public RefreshToken ToDomain()
    {
        return new RefreshToken
        {
            Id = DomainId,
            Token = token,
            UserId = userId,
            ExpiresAt = expiresAt,
            IsRevoked = isRevoked,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }
}