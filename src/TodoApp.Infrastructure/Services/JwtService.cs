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
using TodoApp.Infrastructure.Data;

namespace TodoApp.Infrastructure.Services;

public class JwtService : IJwtService
{
    private readonly JwtSettings _jwtSettings;
    private readonly TodoDbContext _context;
    private readonly SymmetricSecurityKey _key;
    private readonly ILogger<JwtService> _logger;

    public JwtService(IOptions<JwtSettings> jwtSettings, TodoDbContext context, ILogger<JwtService> logger)
    {
        _jwtSettings = jwtSettings.Value;
        _context = context;
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
        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        return token != null && token.IsActive;
    }

    public async Task SaveRefreshTokenAsync(long userId, string refreshToken, DateTime expiresAt)
    {
        // Clean up expired refresh tokens for this user instead of removing all
        var expiredTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync();

        if (expiredTokens.Any())
        {
            _context.RefreshTokens.RemoveRange(expiredTokens);
        }

        // Optional: Limit the number of active refresh tokens per user (e.g., max 5 devices)
        var activeTokensCount = await _context.RefreshTokens
            .CountAsync(rt => rt.UserId == userId && rt.ExpiresAt > DateTime.UtcNow);

        const int maxActiveTokensPerUser = 5;
        if (activeTokensCount >= maxActiveTokensPerUser)
        {
            // Remove the oldest active token to make room for the new one
            var oldestToken = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.ExpiresAt > DateTime.UtcNow)
                .OrderBy(rt => rt.CreatedAt)
                .FirstOrDefaultAsync();

            if (oldestToken != null)
            {
                _context.RefreshTokens.Remove(oldestToken);
                _logger.LogInformation("Removed oldest refresh token for user {UserId} to maintain limit of {MaxTokens}",
                    userId, maxActiveTokensPerUser);
            }
        }

        // Add new refresh token
        var newToken = new RefreshToken
        {
            Token = refreshToken,
            UserId = userId,
            ExpiresAt = expiresAt
        };

        await _context.RefreshTokens.AddAsync(newToken);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Saved new refresh token for user {UserId}. Active tokens count: {Count}",
            userId, activeTokensCount + 1);
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken)
    {
        _logger.LogInformation($"Attempting to revoke refresh token: {refreshToken?.Substring(0, Math.Min(10, refreshToken?.Length ?? 0))}...");

        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (token != null)
        {
            _logger.LogInformation($"Found refresh token with ID: {token.Id}, UserId: {token.UserId}, IsRevoked: {token.IsRevoked}");

            token.Revoke();

            _logger.LogInformation($"Marked token as revoked. IsRevoked: {token.IsRevoked}");

            var changesCount = await _context.SaveChangesAsync();

            _logger.LogInformation($"SaveChanges completed. Changes saved: {changesCount}");
        }
        else
        {
            _logger.LogWarning($"Refresh token not found in database");
        }
    }

    public async Task<long?> GetUserIdFromRefreshTokenAsync(string refreshToken)
    {
        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        return token?.IsActive == true ? token.UserId : null;
    }

    public async Task<IEnumerable<RefreshToken>> GetActiveRefreshTokensAsync(long userId)
    {
        return await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.IsRevoked == false && rt.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(rt => rt.CreatedAt)
            .ToListAsync();
    }

    public async Task RevokeAllRefreshTokensAsync(long userId)
    {
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.IsRevoked == false)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.Revoke();
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Revoked all refresh tokens for user {UserId}. Count: {Count}", userId, tokens.Count);
    }

    public async Task RevokeRefreshTokenByIdAsync(long tokenId)
    {
        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Id == tokenId);

        if (token != null && token.IsActive)
        {
            token.Revoke();
            await _context.SaveChangesAsync();
            _logger.LogInformation("Revoked refresh token with ID: {TokenId} for user {UserId}", tokenId, token.UserId);
        }
        else
        {
            _logger.LogWarning("Attempted to revoke inactive or non-existent refresh token with ID: {TokenId}", tokenId);
        }
    }
}