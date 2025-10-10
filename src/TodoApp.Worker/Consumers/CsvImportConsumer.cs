using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using TodoApp.Application.DTOs;
using TodoApp.Application.Interfaces.Services.DataProcessing;
using TodoApp.Application.Interfaces.Services.Storage;
using TodoApp.Domain.Settings;
using TodoApp.Infrastructure.Services.MessageBroker;

namespace TodoApp.Worker.Consumers;

/// <summary>
/// RabbitMQ consumer that processes CSV import messages
/// Downloads CSV files from MinIO storage and imports todos into PostgreSQL database
/// </summary>
public class CsvImportConsumer : BackgroundService
{
    private readonly RabbitMQSettings _settings;
    private readonly ILogger<CsvImportConsumer> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly RabbitMQConnectionFactory _connectionFactory;
    private IModel? _channel;

    public CsvImportConsumer(
        IOptions<RabbitMQSettings> settings,
        ILogger<CsvImportConsumer> logger,
        IServiceProvider serviceProvider,
        RabbitMQConnectionFactory connectionFactory)
    {
        _settings = settings.Value;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _connectionFactory = connectionFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var workerId = Environment.MachineName ?? Guid.NewGuid().ToString("N")[..8];
        _logger.LogInformation("CSV Import Worker [{WorkerId}] starting...", workerId);

        try
        {
            // Create consumer channel using shared connection factory from Infrastructure
            _channel = _connectionFactory.CreateConsumerChannel();

            // Set up consumer with unique consumer tag per worker instance
            var consumerTag = $"csv-import-consumer-{workerId}";
            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += async (model, ea) =>
            {
                _logger.LogInformation("[{WorkerId}] Received message. DeliveryTag: {DeliveryTag}",
                    workerId, ea.DeliveryTag);
                await ProcessMessageAsync(ea, stoppingToken, workerId);
            };

            // Start consuming messages with unique consumer tag
            _channel.BasicConsume(
                queue: _settings.QueueName,
                autoAck: false, // Manual acknowledgment
                consumer: consumer,
                consumerTag: consumerTag);

            _logger.LogInformation(
                "CSV Import Worker [{WorkerId}] started. Listening on queue: {QueueName}, ConsumerTag: {ConsumerTag}",
                workerId, _settings.QueueName, consumerTag);

            // Keep the service running
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{WorkerId}] Error in CSV Import Worker", workerId);
            throw;
        }
    }

    private async Task ProcessMessageAsync(BasicDeliverEventArgs ea, CancellationToken stoppingToken, string workerId)
    {
        var messageId = ea.BasicProperties.MessageId;
        var messageType = ea.BasicProperties.Headers?.ContainsKey("MessageType") == true
            ? Encoding.UTF8.GetString((byte[])ea.BasicProperties.Headers["MessageType"])
            : "Unknown";

        _logger.LogInformation(
            "[{WorkerId}] Processing message. MessageId: {MessageId}, Type: {MessageType}, DeliveryTag: {DeliveryTag}",
            workerId, messageId, messageType, ea.DeliveryTag);

        try
        {
            var body = ea.Body.ToArray();
            var messageBody = Encoding.UTF8.GetString(body);

            // Deserialize message
            var importMessage = JsonSerializer.Deserialize<ImportMessage>(messageBody);
            if (importMessage == null)
            {
                _logger.LogWarning("[{WorkerId}] Failed to deserialize message. MessageId: {MessageId}", workerId, messageId);
                _channel!.BasicNack(ea.DeliveryTag, false, false); // Don't requeue
                return;
            }

            _logger.LogInformation(
                "[{WorkerId}] Processing import request. RequestId: {RequestId}, UserId: {UserId}, FileName: {FileName}",
                workerId, importMessage.RequestId, importMessage.UserId, importMessage.FileName);

            // Process the import message using scoped services
            using (var scope = _serviceProvider.CreateScope())
            {
                var csvImportService = scope.ServiceProvider
                    .GetRequiredService<ICsvImportService>();
                var storageService = scope.ServiceProvider
                    .GetRequiredService<IFileStorageService>();

                // Download CSV file from storage
                _logger.LogInformation("[{WorkerId}] Downloading CSV from storage: {FileUrl}", workerId, importMessage.FileUrl);
                var csvContent = await storageService.DownloadFileAsync(importMessage.FileUrl);

                // Parse user ID
                if (!long.TryParse(importMessage.UserId, out var userId))
                {
                    _logger.LogError("[{WorkerId}] Invalid UserId format in import message: {UserId}", workerId, importMessage.UserId);
                    _channel!.BasicNack(ea.DeliveryTag, false, false); // Don't requeue
                    return;
                }

                // Import todos
                var result = await csvImportService.ImportTodosAsync(userId, csvContent);

                _logger.LogInformation(
                    "[{WorkerId}] Import completed. RequestId: {RequestId}, Status: {Status}, Imported: {Imported}, Failed: {Failed}",
                    workerId, importMessage.RequestId, result.Status, result.ImportedRecords, result.FailedRecords);

                // Log any errors
                if (result.Errors.Any())
                {
                    _logger.LogWarning(
                        "[{WorkerId}] Import completed with {ErrorCount} errors. RequestId: {RequestId}",
                        workerId, result.FailedRecords, importMessage.RequestId);

                    foreach (var error in result.Errors.Take(10)) // Log first 10 errors
                    {
                        _logger.LogWarning(
                            "[{WorkerId}] Import error for RequestId {RequestId}: {Error}",
                            workerId, importMessage.RequestId, error);
                    }
                }

                // Acknowledge successful processing
                _channel!.BasicAck(ea.DeliveryTag, false);
                _logger.LogInformation("[{WorkerId}] Message processed successfully. MessageId: {MessageId}", workerId, messageId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{WorkerId}] Error processing message. MessageId: {MessageId}", workerId, messageId);

            // Negative acknowledgment with requeue
            // You might want to implement a retry limit or dead-letter queue
            _channel!.BasicNack(ea.DeliveryTag, false, true); // Requeue for retry
        }
    }

    public override void Dispose()
    {
        var workerId = Environment.MachineName ?? "unknown";
        try
        {
            _channel?.Close();
            _channel?.Dispose();
            _logger.LogInformation("[{WorkerId}] CSV Import Worker channel closed", workerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{WorkerId}] Error disposing CSV Import Worker channel", workerId);
        }

        base.Dispose();
    }
}
