using TodoApp.Application.DTOs;

namespace TodoApp.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<LoginResponse> RegisterAsync(RegisterRequest request);
    Task<LoginResponse> RefreshTokenAsync(RefreshTokenRequest request);
    Task<UserDto> GetUserProfileAsync(long userId);
    Task<UserDto> UpdateProfileAsync(long userId, UpdateProfileRequest request);
    Task<bool> ChangePasswordAsync(long userId, ChangePasswordRequest request);
    Task<bool> ValidateTokenAsync(string token);
    Task LogoutAsync(string refreshToken);

    // Multi-device support methods
    Task<IEnumerable<ActiveRefreshTokenDto>> GetActiveSessionsAsync(long userId, string? currentRefreshToken = null);
    Task RevokeSessionAsync(long userId, long tokenId);
    Task RevokeAllOtherSessionsAsync(long userId, string currentRefreshToken);
}