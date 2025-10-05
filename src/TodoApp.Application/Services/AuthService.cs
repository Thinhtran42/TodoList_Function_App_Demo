using Microsoft.Extensions.Logging;
using TodoApp.Application.DTOs;
using TodoApp.Application.Interfaces;
using TodoApp.Application.Mappers;
using TodoApp.Domain.Entities;
using TodoApp.Domain.Exceptions;
using BCrypt.Net;

namespace TodoApp.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IJwtService jwtService,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
        _logger = logger;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new TodoValidationException("Invalid username or password");
        }

        if (!user.IsActive)
        {
            throw new TodoValidationException("Account is deactivated");
        }

        // Update last login
        user.UpdateLastLogin();
        await _userRepository.UpdateAsync(user);

        // Generate tokens
        var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Username);
        var refreshToken = _jwtService.GenerateRefreshToken();
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(7); // 7 days

        // Save refresh token
        await _jwtService.SaveRefreshTokenAsync(user.Id, refreshToken, refreshTokenExpiry);

        _logger.LogInformation("User {Username} logged in successfully", user.Username);

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60), // Based on JWT settings
            User = user.ToDto()
        };
    }

    public async Task<LoginResponse> RegisterAsync(RegisterRequest request)
    {
        // Check if user already exists
        if (await _userRepository.ExistsAsync(request.Username, request.Email))
        {
            throw new TodoValidationException("Username or email already exists");
        }

        // Create user
        var user = request.ToEntity();
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        await _userRepository.CreateAsync(user);

        _logger.LogInformation("New user {Username} registered successfully", user.Username);

        // Auto-login after registration
        return await LoginAsync(new LoginRequest
        {
            Username = request.Username,
            Password = request.Password
        });
    }

    public async Task<LoginResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        if (!await _jwtService.IsRefreshTokenValidAsync(request.RefreshToken))
        {
            throw new TodoValidationException("Invalid or expired refresh token");
        }

        var userId = await _jwtService.GetUserIdFromRefreshTokenAsync(request.RefreshToken);
        if (userId == null)
        {
            throw new TodoValidationException("Invalid refresh token");
        }

        var user = await _userRepository.GetByIdAsync(userId.Value);
        if (user == null || !user.IsActive)
        {
            throw new TodoValidationException("User not found or inactive");
        }

        // Generate new tokens
        var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Username);
        var newRefreshToken = _jwtService.GenerateRefreshToken();
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);

        // Revoke old refresh token and save new one
        await _jwtService.RevokeRefreshTokenAsync(request.RefreshToken);
        await _jwtService.SaveRefreshTokenAsync(user.Id, newRefreshToken, refreshTokenExpiry);

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            User = user.ToDto()
        };
    }

    public async Task<UserDto> GetUserProfileAsync(long userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new TodoNotFoundException("User not found");
        }

        return user.ToDto();
    }

    public async Task<UserDto> UpdateProfileAsync(long userId, UpdateProfileRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new TodoNotFoundException("User not found");
        }

        // Check if email is being changed and if it's already taken
        if (!string.IsNullOrEmpty(request.Email) &&
            request.Email.ToLowerInvariant() != user.Email.ToLowerInvariant())
        {
            var existingUser = await _userRepository.GetByEmailAsync(request.Email);
            if (existingUser != null)
            {
                throw new TodoValidationException("Email already exists");
            }
            user.Email = request.Email.Trim().ToLowerInvariant();
        }

        user.UpdateProfile(request.FirstName, request.LastName);
        await _userRepository.UpdateAsync(user);

        _logger.LogInformation("User {UserId} updated profile", userId);

        return user.ToDto();
    }

    public async Task<bool> ChangePasswordAsync(long userId, ChangePasswordRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new TodoNotFoundException("User not found");
        }

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new TodoValidationException("Current password is incorrect");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdateTimestamp();

        await _userRepository.UpdateAsync(user);

        _logger.LogInformation("User {UserId} changed password", userId);

        return true;
    }

    public Task<bool> ValidateTokenAsync(string token)
    {
        var principal = _jwtService.ValidateToken(token);
        return Task.FromResult(principal != null);
    }

    public async Task LogoutAsync(string refreshToken)
    {
        try
        {
            _logger.LogInformation($"Starting logout process for refresh token");

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                _logger.LogWarning("Refresh token is null or empty");
                return;
            }

            await _jwtService.RevokeRefreshTokenAsync(refreshToken);
            _logger.LogInformation("User logged out successfully, refresh token revoked");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during logout process");
            throw;
        }
    }

    public async Task<IEnumerable<ActiveRefreshTokenDto>> GetActiveSessionsAsync(long userId, string? currentRefreshToken = null)
    {
        try
        {
            var activeTokens = await _jwtService.GetActiveRefreshTokensAsync(userId);

            return activeTokens.Select(token => new ActiveRefreshTokenDto
            {
                Id = token.Id,
                TokenPreview = token.Token.Length > 10 ? token.Token.Substring(0, 10) + "..." : token.Token,
                CreatedAt = token.CreatedAt,
                ExpiresAt = token.ExpiresAt,
                IsCurrent = !string.IsNullOrEmpty(currentRefreshToken) && token.Token == currentRefreshToken
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active sessions for user {UserId}", userId);
            throw;
        }
    }

    public async Task RevokeSessionAsync(long userId, long tokenId)
    {
        try
        {
            // Verify the token belongs to the user for security
            var activeTokens = await _jwtService.GetActiveRefreshTokensAsync(userId);
            var tokenToRevoke = activeTokens.FirstOrDefault(t => t.Id == tokenId);

            if (tokenToRevoke == null)
            {
                throw new UnauthorizedAccessException("Token not found or does not belong to the user");
            }

            await _jwtService.RevokeRefreshTokenByIdAsync(tokenId);
            _logger.LogInformation("Session revoked for user {UserId}, token ID: {TokenId}", userId, tokenId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking session for user {UserId}, token ID: {TokenId}", userId, tokenId);
            throw;
        }
    }

    public async Task RevokeAllOtherSessionsAsync(long userId, string currentRefreshToken)
    {
        try
        {
            var activeTokens = await _jwtService.GetActiveRefreshTokensAsync(userId);
            var tokensToRevoke = activeTokens.Where(t => t.Token != currentRefreshToken);

            foreach (var token in tokensToRevoke)
            {
                await _jwtService.RevokeRefreshTokenByIdAsync(token.Id);
            }

            _logger.LogInformation("Revoked all other sessions for user {UserId}. Count: {Count}",
                userId, tokensToRevoke.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking all other sessions for user {UserId}", userId);
            throw;
        }
    }
}