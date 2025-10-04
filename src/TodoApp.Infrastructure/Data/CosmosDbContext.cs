using Microsoft.EntityFrameworkCore;
using TodoApp.Domain.Entities;
using TodoApp.Infrastructure.Data.Models;

namespace TodoApp.Infrastructure.Data;

public class CosmosDbContext : DbContext
{
    public CosmosDbContext(DbContextOptions<CosmosDbContext> options) : base(options)
    {
    }

    // Use Cosmos-specific models instead of domain entities
    public DbSet<CosmosTodoItem> TodoItems { get; set; }
    public DbSet<CosmosUser> Users { get; set; }
    public DbSet<CosmosRefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure CosmosUser entity
        modelBuilder.Entity<CosmosUser>(entity =>
        {
            entity.ToContainer("Users");
            entity.HasPartitionKey(e => e.DomainId); // Use DomainId as partition key to match existing container
            entity.HasKey(e => e.id); // Use string id as primary key
            
            // Properties already match JSON property names, no need for explicit mapping
        });

        // Configure CosmosTodoItem entity
        modelBuilder.Entity<CosmosTodoItem>(entity =>
        {
            entity.ToContainer("TodoItems");
            entity.HasPartitionKey(e => e.userId); // Partition by userId
            entity.HasKey(e => e.id); // Use string id as primary key
        });

        // Configure CosmosRefreshToken entity
        modelBuilder.Entity<CosmosRefreshToken>(entity =>
        {
            entity.ToContainer("RefreshTokens");
            entity.HasPartitionKey(e => e.userId); // Partition by userId
            entity.HasKey(e => e.id); // Use string id as primary key
        });

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    private void UpdateTimestamps()
    {
        // Update timestamps for Cosmos models
        var cosmosUsers = ChangeTracker.Entries<CosmosUser>();
        var cosmosTodos = ChangeTracker.Entries<CosmosTodoItem>();
        var cosmosTokens = ChangeTracker.Entries<CosmosRefreshToken>();

        foreach (var entry in cosmosUsers)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.createdAt = DateTime.UtcNow;
                    entry.Entity.updatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.updatedAt = DateTime.UtcNow;
                    break;
            }
        }

        foreach (var entry in cosmosTodos)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.createdAt = DateTime.UtcNow;
                    entry.Entity.updatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.updatedAt = DateTime.UtcNow;
                    break;
            }
        }

        foreach (var entry in cosmosTokens)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.createdAt = DateTime.UtcNow;
                    entry.Entity.updatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.updatedAt = DateTime.UtcNow;
                    break;
            }
        }
    }
}