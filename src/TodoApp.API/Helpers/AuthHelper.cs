using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Security.Claims;
using TodoApp.Application.Interfaces;

namespace TodoApp.API.Helpers;

public static class AuthHelper
{
    public static long? GetUserId(FunctionContext context)
    {
        if (context.Items.TryGetValue("UserId", out var userIdObj) && userIdObj is long userId)
        {
            return userId;
        }
        return null;
    }

    public static string? GetUsername(FunctionContext context)
    {
        if (context.Items.TryGetValue("Username", out var usernameObj) && usernameObj is string username)
        {
            return username;
        }
        return null;
    }

    public static ClaimsPrincipal? GetPrincipal(FunctionContext context)
    {
        if (context.Items.TryGetValue("Principal", out var principalObj) && principalObj is ClaimsPrincipal principal)
        {
            return principal;
        }
        return null;
    }

    public static async Task<HttpResponseData> CreateUnauthorizedResponse(HttpRequestData req, string message = "Unauthorized")
    {
        var response = req.CreateResponse(HttpStatusCode.Unauthorized);
        await response.WriteAsJsonAsync(new { error = message });
        return response;
    }

    public static async Task<(bool IsAuthorized, HttpResponseData? Response)> ValidateAuthAsync(FunctionContext context, HttpRequestData req)
    {
        var userId = GetUserId(context);
        if (userId == null)
        {
            var response = await CreateUnauthorizedResponse(req);
            return (false, response);
        }
        return (true, null);
    }

    public static Task<long?> GetUserIdFromTokenAsync(HttpRequestData req, ILogger logger)
    {
        try
        {
            var authHeader = req.Headers.FirstOrDefault(h =>
                h.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase));

            if (authHeader.Value != null && authHeader.Value.Any())
            {
                var token = authHeader.Value.First();
                if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    token = token.Substring("Bearer ".Length).Trim();

                    // Get JWT service from request services
                    var jwtService = req.FunctionContext.InstanceServices.GetService<IJwtService>();
                    if (jwtService == null)
                    {
                        logger.LogError("JWT Service not found in DI container");
                        return Task.FromResult<long?>(null);
                    }

                    var principal = jwtService.ValidateToken(token);
                    if (principal != null)
                    {
                        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        if (long.TryParse(userIdClaim, out var userId))
                        {
                            return Task.FromResult<long?>(userId);
                        }
                    }
                }
            }

            return Task.FromResult<long?>(null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error extracting user ID from token");
            return Task.FromResult<long?>(null);
        }
    }

    /// <summary>
    /// Extract and validate user ID from JWT token (synchronous version)
    /// </summary>
    public static long GetUserIdFromToken(HttpRequestData req, IJwtService jwtService)
    {
        // Check if Authorization header exists
        if (!req.Headers.Contains("Authorization"))
        {
            throw new UnauthorizedAccessException("No authorization header provided");
        }

        var authHeader = req.Headers.GetValues("Authorization").FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            throw new UnauthorizedAccessException("No valid authorization token provided");
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();

        // Validate token signature and get claims principal
        var principal = jwtService.ValidateToken(token);
        if (principal == null)
        {
            throw new UnauthorizedAccessException("Invalid token");
        }

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user ID in token");
        }

        return userId;
    }
}