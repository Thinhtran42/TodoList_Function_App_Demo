using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TodoApp.Application.DTOs;
using TodoApp.Application.Interfaces;
using TodoApp.Application.Services;
using TodoApp.Application.Validators;
using TodoApp.Domain.Entities;
using TodoApp.Infrastructure.Data;
using TodoApp.Infrastructure.Repositories;
using TodoApp.Infrastructure.Repositories.Cosmos;
using TodoApp.Infrastructure.Services;

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
        services.AddScoped<ICsvExportService, CsvExportService>();

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