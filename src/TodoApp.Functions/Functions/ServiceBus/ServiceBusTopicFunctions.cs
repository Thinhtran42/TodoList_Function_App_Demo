using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TodoApp.Application.DTOs;

namespace TodoApp.Functions.Functions;

public class ServiceBusTopicFunctions
{
    private readonly ILogger<ServiceBusTopicFunctions> _logger;

    public ServiceBusTopicFunctions(ILogger<ServiceBusTopicFunctions> logger)
    {
        _logger = logger;
    }

    [Function("ServiceBusTopicTrigger_TodoNotifyHandler")]
    public async Task Run(
        [ServiceBusTrigger("todo-notify-topic", "todo-notification-subscription",
            Connection = "ConnectionStrings:ServiceBus")]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        try
        {
            // Deserialize the message body
            var messageBody = message.Body.ToString();
            var notification = JsonSerializer.Deserialize<TodoNotificationMessage>(messageBody);

            if (notification != null)
            {
                // Simple, concise log message based on event type
                var logMessage = notification.EventType switch
                {
                    "Created" => $"🎉 Todo '{notification.Title}' has been created at {notification.Timestamp:yyyy-MM-dd HH:mm:ss}",
                    "Updated" => $"✏️ Todo '{notification.Title}' has been updated at {notification.Timestamp:yyyy-MM-dd HH:mm:ss}",
                    "Deleted" => $"🗑️ Todo '{notification.Title}' has been deleted at {notification.Timestamp:yyyy-MM-dd HH:mm:ss}",
                    _ => $"📌 Todo '{notification.Title}' event at {notification.Timestamp:yyyy-MM-dd HH:mm:ss}"
                };

                _logger.LogInformation(logMessage);
            }
            else
            {
                _logger.LogWarning("⚠️ Failed to deserialize notification message");
            }

            // Complete the message
            await messageActions.CompleteMessageAsync(message);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "❌ Error deserializing message. Message ID: {MessageId}", message.MessageId);
            // Dead letter the message if it can't be processed
            await messageActions.DeadLetterMessageAsync(
                message,
                new Dictionary<string, object>
                {
                    { "DeadLetterReason", "DeserializationError" },
                    { "DeadLetterErrorDescription", ex.Message }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing notification. Message ID: {MessageId}", message.MessageId);
            // You might want to retry or dead letter based on error type
            throw;
        }
    }
}
