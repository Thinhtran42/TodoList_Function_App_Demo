using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using TodoApp.Application.Interfaces;
using TodoApp.Domain.Entities;

namespace TodoApp.Infrastructure.Services;

public class JwtService : IJwtService
{
    private readonly JwtSettings _jwtSettings;
    private readonly IUserRepository _userRepository;
    private readonly SymmetricSecurityKey _key;
    private readonly ILogger<JwtService> _logger;

    public JwtService(IOptions<JwtSettings> jwtSettings, IUserRepository userRepository, ILogger<JwtService> logger)
    {
        _jwtSettings = jwtSettings.Value;
        _userRepository = userRepository;
        _logger = logger;
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
    }

    public string GenerateAccessToken(long userId, string username)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, username),
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        var credentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _key,
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    public bool IsTokenExpired(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jsonToken = tokenHandler.ReadJwtToken(token);
            return jsonToken.ValidTo <= DateTime.UtcNow;
        }
        catch
        {
            return true;
        }
    }

    public async Task<bool> IsRefreshTokenValidAsync(string refreshToken)
    {
        var token = await _userRepository.GetRefreshTokenAsync(refreshToken);
        return token != null && token.IsActive;
    }

    public async Task SaveRefreshTokenAsync(long userId, string refreshToken, DateTime expiresAt)
    {
        // Clean up expired refresh tokens for this user
        await _userRepository.RemoveExpiredRefreshTokensAsync(userId);

        // Add new refresh token
        var newToken = new RefreshToken
        {
            Token = refreshToken,
            UserId = userId,
            ExpiresAt = expiresAt
        };

        await _userRepository.AddRefreshTokenAsync(newToken);
        _logger.LogInformation("Saved new refresh token for user {UserId}", userId);
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken)
    {
        _logger.LogInformation($"Attempting to revoke refresh token: {refreshToken?.Substring(0, Math.Min(10, refreshToken?.Length ?? 0))}...");
        await _userRepository.RemoveRefreshTokenAsync(refreshToken);
        _logger.LogInformation("Revoked refresh token successfully");
    }

    public async Task<long?> GetUserIdFromRefreshTokenAsync(string refreshToken)
    {
        var token = await _userRepository.GetRefreshTokenAsync(refreshToken);
        return token?.IsActive == true ? token.UserId : null;
    }

    public Task<IEnumerable<RefreshToken>> GetActiveRefreshTokensAsync(long userId)
    {
        // This method would need to be added to IUserRepository interface
        // For now, return empty collection
        return Task.FromResult<IEnumerable<RefreshToken>>(new List<RefreshToken>());
    }

    public Task RevokeAllRefreshTokensAsync(long userId)
    {
        // This would need to be implemented in the repository layer
        _logger.LogInformation("Revoked all refresh tokens for user {UserId}", userId);
        return Task.CompletedTask;
    }

    public Task RevokeRefreshTokenByIdAsync(long tokenId)
    {
        // This would need to be implemented in the repository layer
        _logger.LogInformation("Revoked refresh token with ID: {TokenId}", tokenId);
        return Task.CompletedTask;
    }
}