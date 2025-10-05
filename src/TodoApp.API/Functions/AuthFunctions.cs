using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System.Net;
using TodoApp.API.Helpers;
using TodoApp.Application.DTOs;
using TodoApp.Application.Interfaces;
using TodoApp.Domain.Exceptions;

namespace TodoApp.API.Functions;

public class AuthFunctions
{
    private readonly ILogger<AuthFunctions> _logger;
    private readonly IAuthService _authService;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<ChangePasswordRequest> _changePasswordValidator;
    private readonly IValidator<UpdateProfileRequest> _updateProfileValidator;

    public AuthFunctions(
        ILogger<AuthFunctions> logger,
        IAuthService authService,
        IValidator<LoginRequest> loginValidator,
        IValidator<RegisterRequest> registerValidator,
        IValidator<ChangePasswordRequest> changePasswordValidator,
        IValidator<UpdateProfileRequest> updateProfileValidator)
    {
        _logger = logger;
        _authService = authService;
        _loginValidator = loginValidator;
        _registerValidator = registerValidator;
        _changePasswordValidator = changePasswordValidator;
        _updateProfileValidator = updateProfileValidator;
    }

    [Function("Login")]
    [OpenApiOperation(operationId: "Login", tags: new[] { "Auth" }, Summary = "User login")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(LoginRequest), Required = true)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(LoginResponse))]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(object))]
    public async Task<HttpResponseData> Login(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/login")] HttpRequestData req)
    {
        try
        {
            _logger.LogInformation("User login attempt");

            var request = await req.ReadFromJsonAsync<LoginRequest>();
            if (request == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "Invalid request body" });
                return badResponse;
            }

            // Validate request
            var (isValid, validationResponse) = await ValidationHelper.ValidateAsync(_loginValidator, request, req);
            if (!isValid && validationResponse != null)
            {
                return validationResponse;
            }

            var loginResponse = await _authService.LoginAsync(request);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(loginResponse);
            return response;
        }
        catch (TodoValidationException ex)
        {
            _logger.LogWarning("Login validation error: {Message}", ex.Message);
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(new { error = ex.Message });
            return badResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = "Internal server error" });
            return errorResponse;
        }
    }

    [Function("Register")]
    [OpenApiOperation(operationId: "Register", tags: new[] { "Auth" }, Summary = "User registration")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(RegisterRequest), Required = true)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(LoginResponse))]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(object))]
    public async Task<HttpResponseData> Register(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/register")] HttpRequestData req)
    {
        try
        {
            _logger.LogInformation("User registration attempt");

            var request = await req.ReadFromJsonAsync<RegisterRequest>();
            if (request == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "Invalid request body" });
                return badResponse;
            }

            // Validate request
            var (isValid, validationResponse) = await ValidationHelper.ValidateAsync(_registerValidator, request, req);
            if (!isValid && validationResponse != null)
            {
                return validationResponse;
            }

            var loginResponse = await _authService.RegisterAsync(request);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(loginResponse);
            return response;
        }
        catch (TodoValidationException ex)
        {
            _logger.LogWarning("Registration validation error: {Message}", ex.Message);
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(new { error = ex.Message });
            return badResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = "Internal server error" });
            return errorResponse;
        }
    }

    [Function("RefreshToken")]
    [OpenApiOperation(operationId: "RefreshToken", tags: new[] { "Auth" }, Summary = "Refresh access token")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(RefreshTokenRequest), Required = true)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(LoginResponse))]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(object))]
    public async Task<HttpResponseData> RefreshToken(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/refresh")] HttpRequestData req)
    {
        try
        {
            _logger.LogInformation("Token refresh attempt");

            var request = await req.ReadFromJsonAsync<RefreshTokenRequest>();
            if (request == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "Invalid request body" });
                return badResponse;
            }

            var loginResponse = await _authService.RefreshTokenAsync(request);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(loginResponse);
            return response;
        }
        catch (TodoValidationException ex)
        {
            _logger.LogWarning("Token refresh validation error: {Message}", ex.Message);
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(new { error = ex.Message });
            return badResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = "Internal server error" });
            return errorResponse;
        }
    }

    [Function("GetProfile")]
    [OpenApiOperation(operationId: "GetProfile", tags: new[] { "Auth" }, Summary = "Get user profile")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(UserDto))]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/json", bodyType: typeof(object))]
    public async Task<HttpResponseData> GetProfile(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth/profile")] HttpRequestData req,
        FunctionContext context)
    {
        try
        {
            // Check authentication
            var (isAuthorized, unauthorizedResponse) = await AuthHelper.ValidateAuthAsync(context, req);
            if (!isAuthorized && unauthorizedResponse != null)
            {
                return unauthorizedResponse;
            }

            var userId = AuthHelper.GetUserId(context)!.Value;
            var user = await _authService.GetUserProfileAsync(userId);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(user);
            return response;
        }
        catch (TodoNotFoundException ex)
        {
            _logger.LogWarning("User not found: {Message}", ex.Message);
            var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
            await notFoundResponse.WriteAsJsonAsync(new { error = ex.Message });
            return notFoundResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user profile");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = "Internal server error" });
            return errorResponse;
        }
    }

    [Function("UpdateProfile")]
    [OpenApiOperation(operationId: "UpdateProfile", tags: new[] { "Auth" }, Summary = "Update user profile")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(UpdateProfileRequest), Required = true)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(UserDto))]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/json", bodyType: typeof(object))]
    public async Task<HttpResponseData> UpdateProfile(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "auth/profile")] HttpRequestData req,
        FunctionContext context)
    {
        try
        {
            // Check authentication
            var (isAuthorized, unauthorizedResponse) = await AuthHelper.ValidateAuthAsync(context, req);
            if (!isAuthorized && unauthorizedResponse != null)
            {
                return unauthorizedResponse;
            }

            var request = await req.ReadFromJsonAsync<UpdateProfileRequest>();
            if (request == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "Invalid request body" });
                return badResponse;
            }

            // Validate request
            var (isValid, validationResponse) = await ValidationHelper.ValidateAsync(_updateProfileValidator, request, req);
            if (!isValid && validationResponse != null)
            {
                return validationResponse;
            }

            var userId = AuthHelper.GetUserId(context)!.Value;
            var user = await _authService.UpdateProfileAsync(userId, request);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(user);
            return response;
        }
        catch (TodoValidationException ex)
        {
            _logger.LogWarning("Profile update validation error: {Message}", ex.Message);
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(new { error = ex.Message });
            return badResponse;
        }
        catch (TodoNotFoundException ex)
        {
            _logger.LogWarning("User not found: {Message}", ex.Message);
            var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
            await notFoundResponse.WriteAsJsonAsync(new { error = ex.Message });
            return notFoundResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = "Internal server error" });
            return errorResponse;
        }
    }

    [Function("ChangePassword")]
    [OpenApiOperation(operationId: "ChangePassword", tags: new[] { "Auth" }, Summary = "Change user password")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(ChangePasswordRequest), Required = true)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(object))]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/json", bodyType: typeof(object))]
    public async Task<HttpResponseData> ChangePassword(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/change-password")] HttpRequestData req,
        FunctionContext context)
    {
        try
        {
            // Check authentication
            var (isAuthorized, unauthorizedResponse) = await AuthHelper.ValidateAuthAsync(context, req);
            if (!isAuthorized && unauthorizedResponse != null)
            {
                return unauthorizedResponse;
            }

            var request = await req.ReadFromJsonAsync<ChangePasswordRequest>();
            if (request == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "Invalid request body" });
                return badResponse;
            }

            // Validate request
            var (isValid, validationResponse) = await ValidationHelper.ValidateAsync(_changePasswordValidator, request, req);
            if (!isValid && validationResponse != null)
            {
                return validationResponse;
            }

            var userId = AuthHelper.GetUserId(context)!.Value;
            await _authService.ChangePasswordAsync(userId, request);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new { message = "Password changed successfully" });
            return response;
        }
        catch (TodoValidationException ex)
        {
            _logger.LogWarning("Password change validation error: {Message}", ex.Message);
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(new { error = ex.Message });
            return badResponse;
        }
        catch (TodoNotFoundException ex)
        {
            _logger.LogWarning("User not found: {Message}", ex.Message);
            var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
            await notFoundResponse.WriteAsJsonAsync(new { error = ex.Message });
            return notFoundResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = "Internal server error" });
            return errorResponse;
        }
    }

    [Function("Logout")]
    [OpenApiOperation(operationId: "Logout", tags: new[] { "Auth" }, Summary = "User logout")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(RefreshTokenRequest), Required = true)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(object))]
    public async Task<HttpResponseData> Logout(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/logout")] HttpRequestData req)
    {
        try
        {
            _logger.LogInformation("User logout attempt");

            var request = await req.ReadFromJsonAsync<RefreshTokenRequest>();
            if (request == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "Invalid request body" });
                return badResponse;
            }

            await _authService.LogoutAsync(request.RefreshToken);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new { message = "Logged out successfully" });
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = "Internal server error" });
            return errorResponse;
        }
    }

    [Function("GetActiveSessions")]
    [OpenApiOperation(operationId: "GetActiveSessions", tags: new[] { "Auth" }, Summary = "Get active sessions for current user")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(IEnumerable<ActiveRefreshTokenDto>), Summary = "Active sessions retrieved")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/json", bodyType: typeof(object), Summary = "Unauthorized")]
    public async Task<HttpResponseData> GetActiveSessions(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth/sessions")] HttpRequestData req)
    {
        try
        {
            var userId = await AuthHelper.GetUserIdFromTokenAsync(req, _logger);
            if (!userId.HasValue)
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                await unauthorizedResponse.WriteAsJsonAsync(new { error = "Unauthorized" });
                return unauthorizedResponse;
            }

            // Get current refresh token from Authorization header (if available)
            var currentRefreshToken = req.Headers.GetValues("X-Refresh-Token")?.FirstOrDefault();

            var sessions = await _authService.GetActiveSessionsAsync(userId.Value, currentRefreshToken);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(sessions);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active sessions");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = "Internal server error" });
            return errorResponse;
        }
    }

    [Function("RevokeSession")]
    [OpenApiOperation(operationId: "RevokeSession", tags: new[] { "Auth" }, Summary = "Revoke a specific session")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(RevokeTokenRequest), Required = true, Description = "Token revocation request")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(object), Summary = "Session revoked")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/json", bodyType: typeof(object), Summary = "Unauthorized")]
    public async Task<HttpResponseData> RevokeSession(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/sessions/revoke")] HttpRequestData req)
    {
        try
        {
            var userId = await AuthHelper.GetUserIdFromTokenAsync(req, _logger);
            if (!userId.HasValue)
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                await unauthorizedResponse.WriteAsJsonAsync(new { error = "Unauthorized" });
                return unauthorizedResponse;
            }

            var request = await req.ReadFromJsonAsync<RevokeTokenRequest>();
            if (request == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "Invalid request body" });
                return badResponse;
            }

            await _authService.RevokeSessionAsync(userId.Value, request.TokenId);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new { message = "Session revoked successfully" });
            return response;
        }
        catch (UnauthorizedAccessException)
        {
            var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
            await unauthorizedResponse.WriteAsJsonAsync(new { error = "Token not found or unauthorized" });
            return unauthorizedResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking session");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = "Internal server error" });
            return errorResponse;
        }
    }

    [Function("RevokeAllOtherSessions")]
    [OpenApiOperation(operationId: "RevokeAllOtherSessions", tags: new[] { "Auth" }, Summary = "Revoke all other sessions except current")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(RefreshTokenRequest), Required = true, Description = "Current refresh token")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(object), Summary = "All other sessions revoked")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/json", bodyType: typeof(object), Summary = "Unauthorized")]
    public async Task<HttpResponseData> RevokeAllOtherSessions(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/sessions/revoke-all-others")] HttpRequestData req)
    {
        try
        {
            var userId = await AuthHelper.GetUserIdFromTokenAsync(req, _logger);
            if (!userId.HasValue)
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                await unauthorizedResponse.WriteAsJsonAsync(new { error = "Unauthorized" });
                return unauthorizedResponse;
            }

            var request = await req.ReadFromJsonAsync<RefreshTokenRequest>();
            if (request == null || string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "Invalid request body or missing refresh token" });
                return badResponse;
            }

            await _authService.RevokeAllOtherSessionsAsync(userId.Value, request.RefreshToken);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new { message = "All other sessions revoked successfully" });
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking all other sessions");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = "Internal server error" });
            return errorResponse;
        }
    }
}