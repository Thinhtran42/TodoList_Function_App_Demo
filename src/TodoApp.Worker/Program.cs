using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using TodoApp.Application.Interfaces.Services.MessageBroker;
using TodoApp.Application.Interfaces.Services.Storage;
using TodoApp.Domain.Settings;
using TodoApp.Infrastructure;
using TodoApp.Infrastructure.Data;
using TodoApp.Infrastructure.Services.MessageBroker;
using TodoApp.Infrastructure.Services.Storage;
using TodoApp.Worker.Consumers;
using TodoApp.Worker.Jobs;

var builder = Host.CreateApplicationBuilder(args);

// Get PostgreSQL connection string from appsettings
var connectionString = builder.Configuration.GetConnectionString("TodoDb")
    ?? throw new InvalidOperationException("PostgreSQL connection string is not configured");

// Add PostgreSQL DbContext
builder.Services.AddDbContext<TodoDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add Infrastructure services (repositories, business services, etc.)
builder.Services.AddInfrastructure(connectionString);

// Configure MinIO settings from appsettings
builder.Services.Configure<MinioSettings>(builder.Configuration.GetSection("MinIO"));

// Configure RabbitMQ settings from appsettings
builder.Services.Configure<RabbitMQSettings>(builder.Configuration.GetSection("RabbitMQSettings"));

// Add MinIO Storage Service
builder.Services.AddSingleton<IFileStorageService, MinioStorageService>();

// Add RabbitMQ Connection Factory and Service
builder.Services.AddSingleton<RabbitMQConnectionFactory>();
builder.Services.AddSingleton<IMessageQueueService, RabbitMQService>();

// Configure Hangfire with PostgreSQL storage
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options =>
        options.UseNpgsqlConnection(connectionString)));

// Add Hangfire server with cleanup settings
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 2; // Number of concurrent job workers
    options.ServerName = "TodoApp.Worker";
});

// Register Hangfire background jobs
builder.Services.AddScoped<DatabaseChangeMonitorJob>();

// Add message queue consumers as hosted services
builder.Services.AddHostedService<CsvImportConsumer>();

var host = builder.Build();

// Setup recurring jobs
using (var scope = host.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

    // Remove old jobs if they exist
    recurringJobManager.RemoveIfExists("monitor-all-database-changes");

    // Monitor todo changes every 1 minute
    recurringJobManager.AddOrUpdate<DatabaseChangeMonitorJob>(
        "monitor-todo-changes",
        job => job.MonitorTodoChangesAsync(),
        Cron.Minutely, // Every 1 minute
        new RecurringJobOptions
        {
            TimeZone = TimeZoneInfo.Local // Use local timezone
        });
}

Console.WriteLine("====================================");
Console.WriteLine("🚀 TodoApp Worker Service Started");
Console.WriteLine("====================================");
Console.WriteLine($"⏰ Recurring Jobs Registered:");
Console.WriteLine($"   - monitor-todo-changes: Every 1 minute");
Console.WriteLine($"📊 Hangfire Dashboard: http://localhost:5231/hangfire");
Console.WriteLine($"💡 Tip: Check API service for Hangfire Dashboard");
Console.WriteLine("====================================");

host.Run();
