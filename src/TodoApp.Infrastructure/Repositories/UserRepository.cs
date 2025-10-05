using Microsoft.EntityFrameworkCore;
using TodoApp.Application.Interfaces;
using TodoApp.Domain.Entities;
using TodoApp.Infrastructure.Data;

namespace TodoApp.Infrastructure.Repositories;

public class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(TodoDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await ((TodoDbContext)Context).Users
            .FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await ((TodoDbContext)Context).Users
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());
    }

    public async Task<bool> UserExistsByUsernameAsync(string username)
    {
        return await ((TodoDbContext)Context).Users
            .AnyAsync(u => u.Username == username);
    }

    public async Task<bool> UserExistsByEmailAsync(string email)
    {
        return await ((TodoDbContext)Context).Users
            .AnyAsync(u => u.Email == email.ToLowerInvariant());
    }

    public async Task<bool> ExistsAsync(string username, string email)
    {
        return await ((TodoDbContext)Context).Users
            .AnyAsync(u => u.Username == username || u.Email == email.ToLowerInvariant());
    }

    public async Task<User?> GetByIdWithTodosAsync(long id)
    {
        return await ((TodoDbContext)Context).Users
            .Include(u => u.TodoItems)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    // RefreshToken methods
    public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
    {
        return await ((TodoDbContext)Context).RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token);
    }

    public async Task AddRefreshTokenAsync(RefreshToken refreshToken)
    {
        await ((TodoDbContext)Context).RefreshTokens.AddAsync(refreshToken);
        await Context.SaveChangesAsync();
    }

    public async Task RemoveRefreshTokenAsync(string token)
    {
        var refreshToken = await ((TodoDbContext)Context).RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token);

        if (refreshToken != null)
        {
            ((TodoDbContext)Context).RefreshTokens.Remove(refreshToken);
            await Context.SaveChangesAsync();
        }
    }

    public async Task RemoveExpiredRefreshTokensAsync(long userId)
    {
        var expiredTokens = await ((TodoDbContext)Context).RefreshTokens
            .Where(rt => rt.UserId == userId && rt.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync();

        if (expiredTokens.Any())
        {
            ((TodoDbContext)Context).RefreshTokens.RemoveRange(expiredTokens);
            await Context.SaveChangesAsync();
        }
    }
}