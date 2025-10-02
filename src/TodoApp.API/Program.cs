using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.OpenApi.Models;
using TodoApp.API.Middleware;
using TodoApp.Domain.Entities;
using TodoApp.Infrastructure;

// Azure Functions Application Builder - This will never be null
var builder = FunctionsApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Get connection string
var connectionString = builder.Configuration.GetConnectionString("TodoDb")
    ?? throw new InvalidOperationException("Connection string 'TodoDb' not found.");

// Configure JWT Settings
var jwtSettings = new JwtSettings();
builder.Configuration.GetSection("JwtSettings").Bind(jwtSettings);

// Validate JWT Settings
if (string.IsNullOrEmpty(jwtSettings.SecretKey))
    throw new InvalidOperationException("JWT SecretKey is required.");
if (string.IsNullOrEmpty(jwtSettings.Issuer))
    throw new InvalidOperationException("JWT Issuer is required.");
if (string.IsNullOrEmpty(jwtSettings.Audience))
    throw new InvalidOperationException("JWT Audience is required.");

// Add Infrastructure services (includes DbContext, Repositories, and Services)
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddJwtSettings(jwtSettings);

// Configure OpenAPI với JWT Authentication
builder.Services.AddSingleton<IOpenApiConfigurationOptions>(_ =>
{
    var options = new DefaultOpenApiConfigurationOptions()
    {
        Info = new OpenApiInfo()
        {
            Version = "1.0.0",
            Title = "TodoApp API",
            Description = "A TodoApp API built with Azure Functions and .NET 8 with JWT Authentication.\n\n" +
                         "**To use authentication:**\n" +
                         "1. Call POST /api/auth/login or /api/auth/register to get a JWT token\n" +
                         "2. Click the 'Authorize' button above\n" +
                         "3. Enter: Bearer {your-jwt-token} in the Value field\n" +
                         "4. Click 'Authorize' and then 'Close'\n" +
                         "5. Now you can call protected endpoints"
        },
        OpenApiVersion = Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums.OpenApiVersionType.V3,
        IncludeRequestingHostName = true,
        ForceHttps = false,
        ForceHttp = false
    };

    return options;
});

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();