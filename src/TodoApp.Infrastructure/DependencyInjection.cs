using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TodoApp.Application.DTOs;
using TodoApp.Application.Interfaces.Services.Authentication;
using TodoApp.Application.Interfaces.Services.Business;
using TodoApp.Application.Interfaces.Services.DataProcessing;
using TodoApp.Application.Interfaces.Services.MessageBroker;
using TodoApp.Application.Interfaces.Services.Storage;
using TodoApp.Application.Interfaces.Repositories;
using TodoApp.Application.Services;
using TodoApp.Application.Validators;
using TodoApp.Domain.Entities;
using TodoApp.Domain.Settings;
using TodoApp.Infrastructure.Data;
using TodoApp.Infrastructure.Repositories;
using TodoApp.Infrastructure.Repositories.Cosmos;
using TodoApp.Infrastructure.Services.Persistence;
using TodoApp.Infrastructure.Services.Storage;
using TodoApp.Infrastructure.Services.MessageBroker;

namespace TodoApp.Infrastructure;

public enum DatabaseProvider
{
    PostgreSQL,
    CosmosDB
}

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        // Add PostgreSQL DbContext (default)
        services.AddDbContext<TodoDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Add PostgreSQL Repositories (default)
        services.AddScoped<ITodoRepository, TodoRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        // Add Services
        services.AddScoped<ITodoService, TodoService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<ICsvExportService, CsvExportService>();
        services.AddScoped<ICsvImportService, TodoApp.Application.Services.CsvImportService>();

        // Add Todo Validators
        services.AddScoped<IValidator<CreateTodoRequest>, CreateTodoRequestValidator>();
        services.AddScoped<IValidator<UpdateTodoRequest>, UpdateTodoRequestValidator>();
        services.AddScoped<IValidator<TodoQueryParameters>, TodoQueryParametersValidator>();

        // Add Auth Validators
        services.AddScoped<IValidator<LoginRequest>, LoginRequestValidator>();
        services.AddScoped<IValidator<RegisterRequest>, RegisterRequestValidator>();
        services.AddScoped<IValidator<ChangePasswordRequest>, ChangePasswordRequestValidator>();
        services.AddScoped<IValidator<UpdateProfileRequest>, UpdateProfileRequestValidator>();

        return services;
    }

    public static IServiceCollection AddInfrastructureWithProvider(this IServiceCollection services, IConfiguration configuration, DatabaseProvider provider = DatabaseProvider.PostgreSQL)
    {
        switch (provider)
        {
            case DatabaseProvider.PostgreSQL:
                return services.AddPostgreSQLInfrastructure(configuration.GetConnectionString("TodoDb")!);

            case DatabaseProvider.CosmosDB:
                return services.AddCosmosDBInfrastructure(configuration);

            default:
                throw new ArgumentException($"Unsupported database provider: {provider}");
        }
    }

    public static IServiceCollection AddPostgreSQLInfrastructure(this IServiceCollection services, string connectionString)
    {
        // Add PostgreSQL DbContext
        services.AddDbContext<TodoDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Add PostgreSQL Repositories
        services.AddScoped<ITodoRepository, TodoRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        // Add shared services and validators
        return services.AddSharedServices();
    }

    public static IServiceCollection AddCosmosDBInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add direct Cosmos DB client instead of Entity Framework
        var cosmosConnectionString = configuration.GetConnectionString("CosmosDb");

        services.AddSingleton<Microsoft.Azure.Cosmos.CosmosClient>(serviceProvider =>
        {
            // For local development with Cosmos DB Emulator, bypass SSL certificate validation
            if (cosmosConnectionString?.Contains("localhost") == true || cosmosConnectionString?.Contains("127.0.0.1") == true)
            {
                var options = new Microsoft.Azure.Cosmos.CosmosClientOptions
                {
                    HttpClientFactory = () =>
                    {
                        var handler = new System.Net.Http.HttpClientHandler
                        {
                            // Only for local emulator - bypass SSL validation for self-signed certificates
                            ServerCertificateCustomValidationCallback = System.Net.Http.HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                        };
                        return new System.Net.Http.HttpClient(handler, disposeHandler: true);
                    },
                    ConnectionMode = Microsoft.Azure.Cosmos.ConnectionMode.Gateway
                };
                return new Microsoft.Azure.Cosmos.CosmosClient(cosmosConnectionString, options);
            }

            // Production - use default SSL validation
            return new Microsoft.Azure.Cosmos.CosmosClient(cosmosConnectionString);
        });

        // Add Cosmos DB Repositories using direct client
        services.AddScoped<ITodoRepository, CosmosTodoRepository>(); // Use direct client
        services.AddScoped<IUserRepository, CosmosUserRepository>(); // Use direct client

        // Add shared services and validators
        return services.AddSharedServices();
    }

    public static IServiceCollection AddDualDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        // Add both PostgreSQL and Cosmos DB clients
        var postgresqlConnectionString = configuration.GetConnectionString("TodoDb")!;
        var cosmosConnectionString = configuration.GetConnectionString("CosmosDb")!;
        var cosmosDatabaseName = configuration["CosmosDB:DatabaseName"] ?? "TodoApp";

        services.AddDbContext<TodoDbContext>(options =>
            options.UseNpgsql(postgresqlConnectionString));

        services.AddSingleton<Microsoft.Azure.Cosmos.CosmosClient>(serviceProvider =>
        {
            return new Microsoft.Azure.Cosmos.CosmosClient(cosmosConnectionString);
        });

        // Register both sets of repositories with different lifetimes/keys
        services.AddScoped<TodoRepository>();
        services.AddScoped<CosmosTodoRepository>();
        services.AddScoped<UserRepository>();
        services.AddScoped<CosmosUserRepository>();

        // You can use a factory pattern to choose which repository to use
        services.AddScoped<ITodoRepository>(provider =>
        {
            var databaseProvider = configuration["DatabaseProvider"];
            return databaseProvider?.ToLower() switch
            {
                "cosmosdb" => provider.GetRequiredService<CosmosTodoRepository>(),
                _ => provider.GetRequiredService<TodoRepository>()
            };
        });

        services.AddScoped<IUserRepository>(provider =>
        {
            var databaseProvider = configuration["DatabaseProvider"];
            return databaseProvider?.ToLower() switch
            {
                "cosmosdb" => provider.GetRequiredService<CosmosUserRepository>(),
                _ => provider.GetRequiredService<UserRepository>()
            };
        });

        // Add shared services and validators
        return services.AddSharedServices();
    }

    private static IServiceCollection AddSharedServices(this IServiceCollection services)
    {
        // Add Services
        services.AddScoped<ITodoService, TodoService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<ICsvImportService, TodoApp.Application.Services.CsvImportService>();
        services.AddScoped<ICsvExportService, CsvExportService>();

        // Storage and Message Queue services will be added based on provider configuration
        // See AddStorageServices and AddMessageQueueServices methods

        // Add Todo Validators
        services.AddScoped<IValidator<CreateTodoRequest>, CreateTodoRequestValidator>();
        services.AddScoped<IValidator<UpdateTodoRequest>, UpdateTodoRequestValidator>();
        services.AddScoped<IValidator<TodoQueryParameters>, TodoQueryParametersValidator>();

        // Add Auth Validators
        services.AddScoped<IValidator<LoginRequest>, LoginRequestValidator>();
        services.AddScoped<IValidator<RegisterRequest>, RegisterRequestValidator>();
        services.AddScoped<IValidator<ChangePasswordRequest>, ChangePasswordRequestValidator>();
        services.AddScoped<IValidator<UpdateProfileRequest>, UpdateProfileRequestValidator>();

        return services;
    }

    public static IServiceCollection AddStorageServices(
        this IServiceCollection services,
        IConfiguration configuration,
        StorageProvider storageProvider = StorageProvider.AzureBlob)
    {
        switch (storageProvider)
        {
            case StorageProvider.MinIO:
                // Configure MinIO settings from appsettings
                services.AddOptions<MinioSettings>()
                    .Configure(options =>
                    {
                        options.Endpoint = configuration["MinioSettings:Endpoint"] ?? "localhost:9000";
                        options.AccessKey = configuration["MinioSettings:AccessKey"] ?? "minioadmin";
                        options.SecretKey = configuration["MinioSettings:SecretKey"] ?? "minioadmin";
                        options.BucketName = configuration["MinioSettings:BucketName"] ?? "todo-imports";
                        options.UseSSL = bool.Parse(configuration["MinioSettings:UseSSL"] ?? "false");
                    });
                services.AddScoped<IFileStorageService, MinioStorageService>();
                break;

            case StorageProvider.AzureBlob:
            default:
                // Use Azure Blob Storage (existing implementation)
                services.AddScoped<IFileStorageService, AzureBlobStorageService>();
                break;
        }

        return services;
    }

    public static IServiceCollection AddMessageQueueServices(
        this IServiceCollection services,
        IConfiguration configuration,
        MessageQueueProvider messageQueueProvider = MessageQueueProvider.AzureServiceBus)
    {
        switch (messageQueueProvider)
        {
            case MessageQueueProvider.RabbitMQ:
                // Configure RabbitMQ settings from appsettings
                services.AddOptions<RabbitMQSettings>()
                    .Configure(options =>
                    {
                        options.Host = configuration["RabbitMQSettings:Host"] ?? "localhost";
                        options.Port = int.Parse(configuration["RabbitMQSettings:Port"] ?? "5672");
                        options.Username = configuration["RabbitMQSettings:Username"] ?? "guest";
                        options.Password = configuration["RabbitMQSettings:Password"] ?? "guest";
                        options.QueueName = configuration["RabbitMQSettings:QueueName"] ?? "import-csv-queue";
                        options.ExchangeName = configuration["RabbitMQSettings:ExchangeName"] ?? "todo-exchange";
                        options.RoutingKey = configuration["RabbitMQSettings:RoutingKey"] ?? "todo.import";
                    });
                services.AddScoped<IServiceBusService, RabbitMQService>();
                break;

            case MessageQueueProvider.AzureServiceBus:
            default:
                // Use Azure Service Bus (existing implementation)
                services.AddScoped<IServiceBusService, ServiceBusService>();
                break;
        }

        return services;
    }

    public static IServiceCollection AddJwtSettings(this IServiceCollection services, JwtSettings jwtSettings)
    {
        services.Configure<JwtSettings>(opts =>
        {
            opts.SecretKey = jwtSettings.SecretKey;
            opts.Issuer = jwtSettings.Issuer;
            opts.Audience = jwtSettings.Audience;
            opts.ExpirationInMinutes = jwtSettings.ExpirationInMinutes;
            opts.RefreshTokenExpirationInDays = jwtSettings.RefreshTokenExpirationInDays;
        });

        return services;
    }
}