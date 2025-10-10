using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using FluentValidation;
using Hangfire;
using Hangfire.PostgreSql;
using TodoApp.API.Swagger;
using TodoApp.Domain.Settings;
using TodoApp.Infrastructure;
using TodoApp.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

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

// Determine which storage provider to use
var storageProvider = builder.Configuration["StorageProvider"]?.ToLower() switch
{
    "minio" => StorageProvider.MinIO,
    _ => StorageProvider.AzureBlob
};

// Determine which message queue provider to use
var messageQueueProvider = builder.Configuration["MessageQueueProvider"]?.ToLower() switch
{
    "rabbitmq" => MessageQueueProvider.RabbitMQ,
    _ => MessageQueueProvider.AzureServiceBus
};

// Add Infrastructure services
builder.Services.AddInfrastructureWithProvider(builder.Configuration, databaseProvider);
builder.Services.AddStorageServices(builder.Configuration, storageProvider);
builder.Services.AddMessageQueueServices(builder.Configuration, messageQueueProvider);
builder.Services.AddJwtSettings(jwtSettings);

// Configure Hangfire with PostgreSQL storage (Read-only Dashboard)
var connectionString = builder.Configuration.GetConnectionString("TodoDb")
    ?? throw new InvalidOperationException("PostgreSQL connection string is not configured");

builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options =>
        options.UseNpgsqlConnection(connectionString)));

// Add Hangfire Server for Dashboard visibility
// This server will process jobs with low priority (Worker handles main jobs)
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 1; // Minimum 1 worker required
    options.ServerName = "TodoApp.API.Dashboard";
    options.Queues = new[] { "dashboard-only" }; // Only process jobs from a specific queue (prevents conflicts with Worker)
});

// Add Controllers
builder.Services.AddControllers();

// Add FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<TodoApp.Application.Services.TodoService>();

// Configure JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Configure Swagger with JWT
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TodoApp API",
        Version = "v1",
        Description = "A TodoApp API built with ASP.NET Core Web API and .NET 8 with JWT Authentication"
    });

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "Bearer",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });

    // Add file upload support for Swagger
    c.OperationFilter<FileUploadOperationFilter>();
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TodoApp API V1");
    });

    // Add Hangfire Dashboard (Development only)
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = [] // No authorization in development
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
