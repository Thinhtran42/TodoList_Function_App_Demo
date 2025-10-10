using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TodoApp.Application.DTOs;
using TodoApp.Application.Interfaces.Services.MessageBroker;

namespace TodoApp.Infrastructure.Services.MessageBroker;

/// <summary>
/// Azure Service Bus implementation of IMessageQueueService
/// </summary>
public class ServiceBusService : IMessageQueueService
{
    private readonly ServiceBusClient _serviceBusClient;
    private readonly ServiceBusSender _queueSender;
    private readonly ILogger<ServiceBusService> _logger;
    private const string ImportQueueName = "import-csv-queue";

    public ServiceBusService(IConfiguration configuration, ILogger<ServiceBusService> logger)
    {
        var connectionString = configuration.GetConnectionString("ServiceBus");
        _serviceBusClient = new ServiceBusClient(connectionString);
        _queueSender = _serviceBusClient.CreateSender(ImportQueueName);
        _logger = logger;
    }

    public async Task<string> SendMessageAsync<T>(T message, string? routingKey = null) where T : class
    {
        try
        {
            // Serialize message to JSON
            var messageBody = JsonSerializer.Serialize(message);
            var messageId = Guid.NewGuid().ToString();

            var serviceBusMessage = new ServiceBusMessage(messageBody)
            {
                MessageId = messageId,
                Subject = typeof(T).Name,
                ContentType = "application/json"
            };

            // Add message type and routing information
            serviceBusMessage.ApplicationProperties.Add("MessageType", typeof(T).Name);
            serviceBusMessage.ApplicationProperties.Add("SentAt", DateTime.UtcNow);

            if (!string.IsNullOrEmpty(routingKey))
            {
                serviceBusMessage.ApplicationProperties.Add("RoutingKey", routingKey);
            }

            // Send message to queue
            await _queueSender.SendMessageAsync(serviceBusMessage);

            _logger.LogInformation(
                "Message sent to ServiceBus queue. Type: {MessageType}, MessageId: {MessageId}, Queue: {QueueName}",
                typeof(T).Name, messageId, ImportQueueName);

            return messageId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message to ServiceBus. Type: {MessageType}", typeof(T).Name);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_queueSender != null)
        {
            await _queueSender.DisposeAsync();
        }

        if (_serviceBusClient != null)
        {
            await _serviceBusClient.DisposeAsync();
        }
    }
}