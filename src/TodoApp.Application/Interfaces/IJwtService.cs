using System.Security.Claims;
using TodoApp.Domain.Entities;

namespace TodoApp.Application.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(long userId, string username);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
    bool IsTokenExpired(string token);
    Task<bool> IsRefreshTokenValidAsync(string refreshToken);
    Task SaveRefreshTokenAsync(long userId, string refreshToken, DateTime expiresAt);
    Task RevokeRefreshTokenAsync(string refreshToken);
    Task<long?> GetUserIdFromRefreshTokenAsync(string refreshToken);

    // New methods for multi-device support
    Task<IEnumerable<RefreshToken>> GetActiveRefreshTokensAsync(long userId);
    Task RevokeAllRefreshTokensAsync(long userId);
    Task RevokeRefreshTokenByIdAsync(long tokenId);
}