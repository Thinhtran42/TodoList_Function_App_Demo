using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TodoApp.Application.DTOs;
using TodoApp.Application.Interfaces;
using TodoApp.Application.Services;
using TodoApp.Application.Validators;
using TodoApp.Infrastructure.Data;
using TodoApp.Infrastructure.Repositories;

namespace TodoApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        // Add DbContext
        services.AddDbContext<TodoDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Add Repositories
        services.AddScoped<ITodoRepository, TodoRepository>();
        
        // Add Services
        services.AddScoped<ITodoService, TodoService>();

        // Add Validators
        services.AddScoped<IValidator<CreateTodoRequest>, CreateTodoRequestValidator>();
        services.AddScoped<IValidator<UpdateTodoRequest>, UpdateTodoRequestValidator>();
        services.AddScoped<IValidator<TodoQueryParameters>, TodoQueryParametersValidator>();

        return services;
    }
}