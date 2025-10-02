using TodoApp.Domain.Entities;

namespace TodoApp.Application.Interfaces;

public interface IUserRepository : IBaseRepository<User>
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<bool> ExistsAsync(string username, string email);
    Task<User?> GetByIdWithTodosAsync(long id);
}