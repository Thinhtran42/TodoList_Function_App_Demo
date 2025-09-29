using Microsoft.EntityFrameworkCore;
using TodoApp.Domain.Entities;

namespace TodoApp.Infrastructure.Data;

public abstract class BaseDbContext : DbContext
{
    protected BaseDbContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureBaseEntity(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }

    protected virtual void ConfigureBaseEntity(ModelBuilder modelBuilder)
    {
        // Configure common properties for all entities that inherit from BaseEntity
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType, entity =>
                {
                    entity.Property("Id")
                        .UseIdentityAlwaysColumn()
                        .HasColumnName("id");
                        
                    entity.Property("CreatedAt")
                        .HasDefaultValueSql("now()")
                        .HasColumnName("created_at");
                        
                    entity.Property("UpdatedAt")
                        .HasDefaultValueSql("now()")
                        .HasColumnName("updated_at");
                });
            }
        }
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
        var entities = ChangeTracker.Entries<BaseEntity>();

        foreach (var entry in entities)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }
    }
}