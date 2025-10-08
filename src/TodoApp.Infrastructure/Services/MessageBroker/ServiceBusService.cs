using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TodoApp.Application.DTOs;
using TodoApp.Application.Interfaces.Services.MessageBroker;

namespace TodoApp.Infrastructure.Services.MessageBroker;

public class ServiceBusService : IServiceBusService
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

    public async Task<string> SendImportMessageAsync(ImportMessage message)
    {
        try
        {
            // Serialize message to JSON
            var messageBody = JsonSerializer.Serialize(message);
            var serviceBusMessage = new ServiceBusMessage(messageBody)
            {
                MessageId = message.RequestId,
                Subject = "CSV Import Request",
                ContentType = "application/json"
            };

            // Add custom properties for filtering/routing
            serviceBusMessage.ApplicationProperties.Add("UserId", message.UserId);
            serviceBusMessage.ApplicationProperties.Add("RequestedAt", message.RequestedAt);

            // Send message to queue
            await _queueSender.SendMessageAsync(serviceBusMessage);

            _logger.LogInformation("Import message sent to ServiceBus queue. RequestId: {RequestId}, UserId: {UserId}",
                message.RequestId, message.UserId);

            return message.RequestId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send import message to ServiceBus. RequestId: {RequestId}, UserId: {UserId}",
                message.RequestId, message.UserId);
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