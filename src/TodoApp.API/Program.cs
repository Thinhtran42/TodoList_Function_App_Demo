using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using TodoApp.Infrastructure;

// Azure Functions Application Builder - This will never be null
var builder = FunctionsApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Get connection string
var connectionString = builder.Configuration.GetConnectionString("TodoDb")
    ?? throw new InvalidOperationException("Connection string 'TodoDb' not found.");

// Add Infrastructure services (includes DbContext, Repositories, and Services)
builder.Services.AddInfrastructure(connectionString);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();