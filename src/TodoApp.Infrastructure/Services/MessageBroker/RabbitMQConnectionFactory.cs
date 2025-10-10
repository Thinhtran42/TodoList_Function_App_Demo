using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using TodoApp.Domain.Settings;

namespace TodoApp.Infrastructure.Services.MessageBroker;

/// <summary>
/// Singleton factory for creating and managing RabbitMQ connections
/// Shared between producers (RabbitMQService) and consumers (Worker)
/// </summary>
public class RabbitMQConnectionFactory : IDisposable
{
    private readonly RabbitMQSettings _settings;
    private readonly ILogger<RabbitMQConnectionFactory> _logger;
    private IConnection? _connection;
    private readonly object _lock = new();

    public RabbitMQConnectionFactory(
        IOptions<RabbitMQSettings> settings,
        ILogger<RabbitMQConnectionFactory> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Get or create shared RabbitMQ connection (thread-safe)
    /// </summary>
    public IConnection GetConnection()
    {
        if (_connection != null && _connection.IsOpen)
        {
            return _connection;
        }

        lock (_lock)
        {
            // Double-check after acquiring lock
            if (_connection != null && _connection.IsOpen)
            {
                return _connection;
            }

            _logger.LogInformation("Creating new RabbitMQ connection to {Host}:{Port}",
                _settings.Host, _settings.Port);

            var factory = new ConnectionFactory
            {
                HostName = _settings.Host,
                Port = _settings.Port,
                UserName = _settings.Username,
                Password = _settings.Password,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                RequestedHeartbeat = TimeSpan.FromSeconds(60)
            };

            try
            {
                _connection = factory.CreateConnection();
                _logger.LogInformation("RabbitMQ connection established successfully");
                return _connection;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create RabbitMQ connection");
                throw;
            }
        }
    }

    /// <summary>
    /// Create a new channel for message producer
    /// </summary>
    public IModel CreateProducerChannel()
    {
        var connection = GetConnection();
        var channel = connection.CreateModel();

        try
        {
            // Declare exchange (ensure it exists)
            channel.ExchangeDeclare(
                exchange: _settings.ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            // Declare queue (ensure it exists)
            channel.QueueDeclare(
                queue: _settings.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            // Bind queue to exchange
            channel.QueueBind(
                queue: _settings.QueueName,
                exchange: _settings.ExchangeName,
                routingKey: _settings.RoutingKey);

            _logger.LogInformation(
                "Producer channel created. Exchange: {Exchange}, Queue: {Queue}",
                _settings.ExchangeName, _settings.QueueName);

            return channel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure producer channel");
            channel?.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Create a new channel for message consumer with QoS settings
    /// </summary>
    public IModel CreateConsumerChannel()
    {
        var connection = GetConnection();
        var channel = connection.CreateModel();

        try
        {
            // Declare exchange (ensure it exists)
            channel.ExchangeDeclare(
                exchange: _settings.ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            // Declare queue (ensure it exists)
            channel.QueueDeclare(
                queue: _settings.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            // Bind queue to exchange
            channel.QueueBind(
                queue: _settings.QueueName,
                exchange: _settings.ExchangeName,
                routingKey: _settings.RoutingKey);

            // Set prefetch count (QoS) - process one message at a time
            channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            _logger.LogInformation(
                "Consumer channel created. Exchange: {Exchange}, Queue: {Queue}, RoutingKey: {RoutingKey}",
                _settings.ExchangeName, _settings.QueueName, _settings.RoutingKey);

            return channel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure consumer channel");
            channel?.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        try
        {
            if (_connection != null && _connection.IsOpen)
            {
                _connection.Close();
                _connection.Dispose();
                _logger.LogInformation("RabbitMQ connection closed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing RabbitMQ connection");
        }
    }
}
