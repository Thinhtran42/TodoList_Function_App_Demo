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
    private readonly IModel _channel;

    public RabbitMQService(
        IOptions<RabbitMQSettings> settings,
        ILogger<RabbitMQService> logger,
        RabbitMQConnectionFactory connectionFactory)
    {
        _settings = settings.Value;
        _logger = logger;

        try
        {
            // Use shared connection factory to create producer channel
            _channel = connectionFactory.CreateProducerChannel();
            _logger.LogInformation("RabbitMQ producer service initialized");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ producer service");
            throw;
        }
    }

    public async Task<string> SendMessageAsync<T>(T message, string? routingKey = null) where T : class
    {
        try
        {
            var messageBody = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(messageBody);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true; // Make message persistent
            properties.ContentType = "application/json";
            properties.MessageId = Guid.NewGuid().ToString();

            // Use provided routing key or default from settings
            var effectiveRoutingKey = routingKey ?? _settings.RoutingKey;

            // Add type information for better debugging and routing
            properties.Headers = new Dictionary<string, object>
            {
                { "MessageType", typeof(T).Name },
                { "SentAt", DateTime.UtcNow.ToString("o") }
            };

            // Publish message to exchange
            _channel.BasicPublish(
                exchange: _settings.ExchangeName,
                routingKey: effectiveRoutingKey,
                basicProperties: properties,
                body: body);

            _logger.LogInformation(
                "Message sent to RabbitMQ. Type: {MessageType}, MessageId: {MessageId}, Exchange: {Exchange}, RoutingKey: {RoutingKey}",
                typeof(T).Name, properties.MessageId, _settings.ExchangeName, effectiveRoutingKey);

            await Task.CompletedTask;
            return properties.MessageId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message to RabbitMQ. Type: {MessageType}", typeof(T).Name);
            throw;
        }
    }

    public void Dispose()
    {
        try
        {
            _channel?.Close();
            _channel?.Dispose();
            _logger.LogInformation("RabbitMQ producer channel closed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing RabbitMQ producer channel");
        }
    }
}
