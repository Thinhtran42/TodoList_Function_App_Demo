using Hangfire;
using Microsoft.Extensions.Logging;
using TodoApp.Application.Interfaces.Repositories;

namespace TodoApp.Worker.Jobs;

/// <summary>
/// Hangfire recurring job to monitor todo changes in PostgreSQL
/// Tracks new and updated todo items
/// </summary>
public class DatabaseChangeMonitorJob
{
    private readonly ITodoRepository _todoRepository;
    private readonly ILogger<DatabaseChangeMonitorJob> _logger;
    private static DateTime _lastCheckTime = DateTime.UtcNow;

    public DatabaseChangeMonitorJob(
        ITodoRepository todoRepository,
        ILogger<DatabaseChangeMonitorJob> logger)
    {
        _todoRepository = todoRepository;
        _logger = logger;
    }

    /// <summary>
    /// Monitors for new or updated todos since last check
    /// This job runs every minute by default
    /// Succeeded jobs will be automatically deleted after 1 hour
    /// </summary>
    [AutomaticRetry(Attempts = 2)]
    [DisableConcurrentExecution(timeoutInSeconds: 60)]
    public async Task MonitorTodoChangesAsync()
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation(
            "Starting database change monitor. Last check: {LastCheckTime} UTC (Local: {LocalTime})",
            _lastCheckTime, _lastCheckTime.ToLocalTime());

        try
        {
            // Get all todos to check for changes
            // In production, you would use a more efficient query with a WHERE clause
            // filtering by updated_at > _lastCheckTime
            var allTodos = await _todoRepository.GetAllAsync();

            // Find newly created todos
            var newTodos = allTodos.Where(t => t.CreatedAt > _lastCheckTime).ToList();
            if (newTodos.Any())
            {
                _logger.LogInformation(
                    "Found {Count} new todo(s) created since {LastCheckTime}",
                    newTodos.Count, _lastCheckTime);

                foreach (var todo in newTodos.Take(10)) // Log first 10
                {
                    _logger.LogInformation(
                        "New Todo: Id={Id}, Title={Title}, CreatedBy={UserId}, CreatedAt={CreatedAt}",
                        todo.Id, todo.Title, todo.UserId, todo.CreatedAt);
                }
            }

            // Find updated todos
            var updatedTodos = allTodos
                .Where(t => t.UpdatedAt > _lastCheckTime && t.CreatedAt <= _lastCheckTime)
                .ToList();

            if (updatedTodos.Any())
            {
                _logger.LogInformation(
                    "Found {Count} updated todo(s) since {LastCheckTime}",
                    updatedTodos.Count, _lastCheckTime);

                foreach (var todo in updatedTodos.Take(10)) // Log first 10
                {
                    _logger.LogInformation(
                        "Updated Todo: Id={Id}, Title={Title}, UpdatedBy={UserId}, UpdatedAt={UpdatedAt}",
                        todo.Id, todo.Title, todo.UserId, todo.UpdatedAt);
                }
            }

            if (!newTodos.Any() && !updatedTodos.Any())
            {
                _logger.LogDebug("No database changes detected since {LastCheckTime} UTC (Local: {LocalTime})",
                    _lastCheckTime, _lastCheckTime.ToLocalTime());
            }

            // Update last check time
            _lastCheckTime = startTime;

            _logger.LogInformation(
                "Database change monitor completed. Duration: {Duration}ms",
                (DateTime.UtcNow - startTime).TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error monitoring todo changes");
            throw;
        }
    }
}
