namespace TodoApp.Domain.Settings;

public enum StorageProvider
{
    AzureBlob,
    MinIO
}

public enum MessageQueueProvider
{
    AzureServiceBus,
    RabbitMQ
}
