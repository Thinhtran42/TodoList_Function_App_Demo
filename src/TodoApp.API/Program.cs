using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.OpenApi.Models;
using TodoApp.API.Middleware;
using TodoApp.Domain.Entities;
using TodoApp.Infrastructure;
using TodoApp.Infrastructure.Extensions;

// Azure Functions Application Builder - This will never be null
var builder = FunctionsApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

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

// Determine which database provider to use
var databaseProvider = builder.Configuration["DatabaseProvider"]?.ToLower() switch
{
    "cosmosdb" => DatabaseProvider.CosmosDB,
    _ => DatabaseProvider.PostgreSQL
};

// Add Infrastructure services based on provider
builder.Services.AddInfrastructureWithProvider(builder.Configuration, databaseProvider);
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

var app = builder.Build();

// Initialize database if using Cosmos DB
if (databaseProvider == DatabaseProvider.CosmosDB)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    
    try
    {
        var cosmosClient = services.GetRequiredService<Microsoft.Azure.Cosmos.CosmosClient>();
        var logger = services.GetRequiredService<ILogger<Program>>();
        
        logger.LogInformation("Initializing Cosmos DB...");
        
        // Initialize database and containers using direct Cosmos SDK
        var databaseName = builder.Configuration["CosmosDB:DatabaseName"] ?? "TodoApp";
        var database = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseName);
        
        // Create containers with proper partition keys
        await database.Database.CreateContainerIfNotExistsAsync("Users", "/DomainId");
        await database.Database.CreateContainerIfNotExistsAsync("TodoItems", "/userId");
        await database.Database.CreateContainerIfNotExistsAsync("RefreshTokens", "/userId");
        
        logger.LogInformation("✅ Cosmos DB initialized successfully!");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "❌ Error initializing Cosmos DB: {Message}", ex.Message);
        throw;
    }
}

app.Run();