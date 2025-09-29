using Microsoft.EntityFrameworkCore;
using TodoApp.Application.Interfaces;
using TodoApp.Domain.Entities;
using TodoApp.Infrastructure.Data;

namespace TodoApp.Infrastructure.Repositories;

public abstract class BaseRepository<TEntity> : IBaseRepository<TEntity> where TEntity : BaseEntity
{
    protected readonly BaseDbContext Context;
    protected readonly DbSet<TEntity> DbSet;

    protected BaseRepository(BaseDbContext context)
    {
        Context = context;
        DbSet = context.Set<TEntity>();
    }

    public virtual async Task<TEntity?> GetByIdAsync(long id)
    {
        return await DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync()
    {
        return await DbSet
            .AsNoTracking()
            .OrderByDescending(x => x.Id)
            .ToListAsync();
    }

    public virtual async Task<TEntity> CreateAsync(TEntity entity)
    {
        DbSet.Add(entity);
        await Context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task<TEntity?> UpdateAsync(TEntity entity)
    {
        var existingEntity = await DbSet.FindAsync(entity.Id);
        if (existingEntity == null)
        {
            return null;
        }

        // Update the entity using EF's ChangeTracker
        Context.Entry(existingEntity).CurrentValues.SetValues(entity);
        await Context.SaveChangesAsync();
        
        return existingEntity;
    }

    public virtual async Task<bool> DeleteAsync(long id)
    {
        var entity = await DbSet.FindAsync(id);
        if (entity == null)
        {
            return false;
        }

        DbSet.Remove(entity);
        await Context.SaveChangesAsync();
        return true;
    }

    public virtual async Task<bool> ExistsAsync(long id)
    {
        return await DbSet.AnyAsync(x => x.Id == id);
    }
}