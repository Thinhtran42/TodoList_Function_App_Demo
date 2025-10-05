using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using TodoApp.Application.Interfaces;

namespace TodoApp.API.Middleware;

public class JwtMiddleware : IFunctionsWorkerMiddleware
{
    private readonly IJwtService _jwtService;
    private readonly ILogger<JwtMiddleware> _logger;

    public JwtMiddleware(IJwtService jwtService, ILogger<JwtMiddleware> logger)
    {
        _jwtService = jwtService;
        _logger = logger;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        try
        {
            var request = await context.GetHttpRequestDataAsync();
            if (request != null)
            {
                var authHeader = request.Headers.FirstOrDefault(h =>
                    h.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase));

                if (authHeader.Value != null && authHeader.Value.Any())
                {
                    var token = authHeader.Value.First();
                    if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        token = token.Substring("Bearer ".Length).Trim();

                        var principal = _jwtService.ValidateToken(token);
                        if (principal != null)
                        {
                            // Add user information to context
                            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                            var usernameClaim = principal.FindFirst(ClaimTypes.Name)?.Value;

                            if (long.TryParse(userIdClaim, out var userId))
                            {
                                context.Items["UserId"] = userId;
                                context.Items["Username"] = usernameClaim;
                                context.Items["Principal"] = principal;
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error validating JWT token");
        }

        await next(context);
    }
}