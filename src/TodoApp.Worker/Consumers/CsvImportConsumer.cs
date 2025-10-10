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
        _logger.LogInformation("CSV Import Worker starting...");

        try
        {
            // Create consumer channel using shared connection factory from Infrastructure
            _channel = _connectionFactory.CreateConsumerChannel();

            // Set up consumer
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                await ProcessMessageAsync(ea, stoppingToken);
            };

            // Start consuming messages
            _channel.BasicConsume(
                queue: _settings.QueueName,
                autoAck: false, // Manual acknowledgment
                consumer: consumer);

            _logger.LogInformation(
                "CSV Import Worker started. Listening on queue: {QueueName}",
                _settings.QueueName);

            // Keep the service running
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CSV Import Worker");
            throw;
        }
    }

    private async Task ProcessMessageAsync(BasicDeliverEventArgs ea, CancellationToken stoppingToken)
    {
        var messageId = ea.BasicProperties.MessageId;
        var messageType = ea.BasicProperties.Headers?.ContainsKey("MessageType") == true
            ? Encoding.UTF8.GetString((byte[])ea.BasicProperties.Headers["MessageType"])
            : "Unknown";

        _logger.LogInformation(
            "Received message. MessageId: {MessageId}, Type: {MessageType}",
            messageId, messageType);

        try
        {
            var body = ea.Body.ToArray();
            var messageBody = Encoding.UTF8.GetString(body);

            // Deserialize message
            var importMessage = JsonSerializer.Deserialize<ImportMessage>(messageBody);
            if (importMessage == null)
            {
                _logger.LogWarning("Failed to deserialize message. MessageId: {MessageId}", messageId);
                _channel!.BasicNack(ea.DeliveryTag, false, false); // Don't requeue
                return;
            }

            _logger.LogInformation(
                "Processing import request. RequestId: {RequestId}, UserId: {UserId}, FileName: {FileName}",
                importMessage.RequestId, importMessage.UserId, importMessage.FileName);

            // Process the import message using scoped services
            using (var scope = _serviceProvider.CreateScope())
            {
                var csvImportService = scope.ServiceProvider
                    .GetRequiredService<ICsvImportService>();
                var storageService = scope.ServiceProvider
                    .GetRequiredService<IFileStorageService>();

                // Download CSV file from storage
                _logger.LogInformation("Downloading CSV from storage: {FileUrl}", importMessage.FileUrl);
                var csvContent = await storageService.DownloadFileAsync(importMessage.FileUrl);

                // Parse user ID
                if (!long.TryParse(importMessage.UserId, out var userId))
                {
                    _logger.LogError("Invalid UserId format in import message: {UserId}", importMessage.UserId);
                    _channel!.BasicNack(ea.DeliveryTag, false, false); // Don't requeue
                    return;
                }

                // Import todos
                var result = await csvImportService.ImportTodosAsync(userId, csvContent);

                _logger.LogInformation(
                    "Import completed. RequestId: {RequestId}, Status: {Status}, Imported: {Imported}, Failed: {Failed}",
                    importMessage.RequestId, result.Status, result.ImportedRecords, result.FailedRecords);

                // Log any errors
                if (result.Errors.Any())
                {
                    _logger.LogWarning(
                        "Import completed with {ErrorCount} errors. RequestId: {RequestId}",
                        result.FailedRecords, importMessage.RequestId);

                    foreach (var error in result.Errors.Take(10)) // Log first 10 errors
                    {
                        _logger.LogWarning(
                            "Import error for RequestId {RequestId}: {Error}",
                            importMessage.RequestId, error);
                    }
                }

                // Acknowledge successful processing
                _channel!.BasicAck(ea.DeliveryTag, false);
                _logger.LogInformation("Message processed successfully. MessageId: {MessageId}", messageId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message. MessageId: {MessageId}", messageId);

            // Negative acknowledgment with requeue
            // You might want to implement a retry limit or dead-letter queue
            _channel!.BasicNack(ea.DeliveryTag, false, true); // Requeue for retry
        }
    }

    public override void Dispose()
    {
        try
        {
            _channel?.Close();
            _channel?.Dispose();
            _logger.LogInformation("CSV Import Worker channel closed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing CSV Import Worker channel");
        }

        base.Dispose();
    }
}
