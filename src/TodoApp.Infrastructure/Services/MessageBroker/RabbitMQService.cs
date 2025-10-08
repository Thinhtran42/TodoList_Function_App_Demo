using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using TodoApp.Application.DTOs;
using TodoApp.Application.Interfaces.Services.MessageBroker;
using TodoApp.Domain.Settings;

namespace TodoApp.Infrastructure.Services.MessageBroker;

/// <summary>
/// RabbitMQ implementation of IMessageQueueService
/// </summary>
public class RabbitMQService : IMessageQueueService, IDisposable
{
    private readonly RabbitMQSettings _settings;
    private readonly ILogger<RabbitMQService> _logger;
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public RabbitMQService(
        IOptions<RabbitMQSettings> settings,
        ILogger<RabbitMQService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        try
        {
            // Create connection factory
            var factory = new ConnectionFactory
            {
                HostName = _settings.Host,
                Port = _settings.Port,
                UserName = _settings.Username,
                Password = _settings.Password,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            // Create connection and channel
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declare exchange (topic type for routing)
            _channel.ExchangeDeclare(
                exchange: _settings.ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            // Declare queue
            _channel.QueueDeclare(
                queue: _settings.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            // Bind queue to exchange
            _channel.QueueBind(
                queue: _settings.QueueName,
                exchange: _settings.ExchangeName,
                routingKey: _settings.RoutingKey);

            _logger.LogInformation("RabbitMQ connection established. Exchange: {Exchange}, Queue: {Queue}",
                _settings.ExchangeName, _settings.QueueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to RabbitMQ");
            throw;
        }
    }

    public async Task<string> SendImportMessageAsync(ImportMessage message)
    {
        try
        {
            var messageBody = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(messageBody);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true; // Make message persistent
            properties.ContentType = "application/json";
            properties.MessageId = message.RequestId;

            // Add custom headers
            properties.Headers = new Dictionary<string, object>
            {
                { "RequestId", message.RequestId },
                { "UserId", message.UserId },
                { "RequestedAt", message.RequestedAt.ToString("o") }
            };

            // Publish message to exchange
            _channel.BasicPublish(
                exchange: _settings.ExchangeName,
                routingKey: _settings.RoutingKey,
                basicProperties: properties,
                body: body);

            _logger.LogInformation(
                "Import message sent to RabbitMQ. RequestId: {RequestId}, UserId: {UserId}, Exchange: {Exchange}, RoutingKey: {RoutingKey}",
                message.RequestId, message.UserId, _settings.ExchangeName, _settings.RoutingKey);

            await Task.CompletedTask;
            return message.RequestId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send import message to RabbitMQ. RequestId: {RequestId}, UserId: {UserId}",
                message.RequestId, message.UserId);
            throw;
        }
    }

    public void Dispose()
    {
        try
        {
            _channel?.Close();
            _channel?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
            _logger.LogInformation("RabbitMQ connection closed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing RabbitMQ connection");
        }
    }
}
