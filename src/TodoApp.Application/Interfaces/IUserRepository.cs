using TodoApp.Domain.Entities;

namespace TodoApp.Application.Interfaces;

public interface IUserRepository : IBaseRepository<User>
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<bool> UserExistsByUsernameAsync(string username);
    Task<bool> UserExistsByEmailAsync(string email);
    Task<bool> ExistsAsync(string username, string email);
    Task<User?> GetByIdWithTodosAsync(long id);
    
    // RefreshToken methods
    Task<RefreshToken?> GetRefreshTokenAsync(string token);
    Task AddRefreshTokenAsync(RefreshToken refreshToken);
    Task RemoveRefreshTokenAsync(string token);
    Task RemoveExpiredRefreshTokensAsync(long userId);
}