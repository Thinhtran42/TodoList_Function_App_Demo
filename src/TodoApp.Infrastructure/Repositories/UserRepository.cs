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
}