using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TodoApp.Application.DTOs;
using TodoApp.Domain.Enums;
using TodoApp.Infrastructure.Data.Models;

namespace TodoApp.Functions.Functions;

public class TodoChangeFeedFunctions
{
    private readonly ILogger<TodoChangeFeedFunctions> _logger;
    private readonly ServiceBusClient _serviceBusClient;
    private readonly IConfiguration _configuration;
    private const string TopicName = "todo-notify-topic";

    public TodoChangeFeedFunctions(
        ILogger<TodoChangeFeedFunctions> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;

        var serviceBusConnection = configuration.GetConnectionString("ServiceBus");
        _serviceBusClient = new ServiceBusClient(serviceBusConnection);
    }

    [Function("TodoChangeFeedTrigger")]
    public async Task Run(
        [CosmosDBTrigger(
            databaseName: "TodoApp",
            containerName: "TodoItems",
            Connection = "ConnectionStrings:CosmosDb",
            LeaseContainerName = "leases",
            CreateLeaseContainerIfNotExists = true)]
        IReadOnlyList<CosmosTodoItem> input)
    {
        try
        {
            if (input != null && input.Count > 0)
            {
                _logger.LogInformation("=== Cosmos DB Change Feed Triggered ===");
                _logger.LogInformation("Documents modified: {Count}", input.Count);

                var sender = _serviceBusClient.CreateSender(TopicName);

                foreach (var document in input)
                {
                    try
                    {
                        _logger.LogInformation("Processing change for Todo: {TodoId}, Title: {Title}, UserId: {UserId}",
                            document.DomainId, document.title, document.userId);

                        // Create notification message
                        var notification = new TodoNotificationMessage
                        {
                            EventType = DetermineEventType(document),
                            TodoId = document.DomainId,
                            Title = document.title,
                            UserId = document.userId,
                            Timestamp = DateTime.UtcNow,
                            Priority = (Priority)document.priority,
                            Category = (Category)document.category,
                            IsCompleted = document.isCompleted
                        };

                        // Serialize to JSON
                        var messageBody = JsonSerializer.Serialize(notification);
                        var serviceBusMessage = new ServiceBusMessage(messageBody)
                        {
                            ContentType = "application/json",
                            Subject = notification.EventType,
                            MessageId = Guid.NewGuid().ToString(),
                            ApplicationProperties =
                            {
                                ["TodoId"] = notification.TodoId,
                                ["UserId"] = notification.UserId,
                                ["EventType"] = notification.EventType
                            }
                        };

                        // Send to Service Bus Topic
                        await sender.SendMessageAsync(serviceBusMessage);

                        _logger.LogInformation("✅ Sent notification to Service Bus Topic. TodoId: {TodoId}, EventType: {EventType}",
                            notification.TodoId, notification.EventType);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing document with ID: {DocumentId}", document.id);
                        // Continue processing other documents
                    }
                }

                await sender.DisposeAsync();
                _logger.LogInformation("=== Change Feed Processing Completed ===");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Change Feed Trigger");
            throw;
        }
    }

    private string DetermineEventType(CosmosTodoItem document)
    {
        // Simple logic: if createdAt and updatedAt are very close, it's a create
        // Otherwise it's an update. Deletion would need to be handled differently
        // (would need to track deletions or use soft deletes)

        var timeDiff = (document.updatedAt - document.createdAt).TotalSeconds;

        if (timeDiff < 2) // Within 2 seconds - likely a create
        {
            return "Created";
        }
        else
        {
            return "Updated";
        }
    }
}
