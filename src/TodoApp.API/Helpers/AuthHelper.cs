using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace TodoApp.API.Helpers;

/// <summary>
/// Helper methods for authentication in ASP.NET Core Web API
/// </summary>
public static class AuthHelper
{
    /// <summary>
    /// Get user ID from ClaimsPrincipal
    /// </summary>
    public static long? GetUserId(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (long.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }

    /// <summary>
    /// Get user ID from HttpContext (throws if not authenticated)
    /// </summary>
    public static long GetUserIdOrThrow(HttpContext context)
    {
        var userId = GetUserId(context.User);
        if (userId == null)
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return userId.Value;
    }

    /// <summary>
    /// Get username from ClaimsPrincipal
    /// </summary>
    public static string? GetUsername(ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Name)?.Value;
    }

    /// <summary>
    /// Get email from ClaimsPrincipal
    /// </summary>
    public static string? GetEmail(ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Email)?.Value;
    }

    /// <summary>
    /// Check if user is authenticated
    /// </summary>
    public static bool IsAuthenticated(ClaimsPrincipal user)
    {
        return user.Identity?.IsAuthenticated ?? false;
    }

    /// <summary>
    /// Get all user claims as dictionary
    /// </summary>
    public static Dictionary<string, string> GetUserClaims(ClaimsPrincipal user)
    {
        return user.Claims.ToDictionary(
            c => c.Type,
            c => c.Value
        );
    }
}