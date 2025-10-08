using TodoApp.Application.DTOs;

namespace TodoApp.Application.Interfaces.Services.MessageBroker;

/// <summary>
/// Interface for message queue operations (Azure Service Bus, RabbitMQ, etc.)
/// </summary>
public interface IMessageQueueService
{
    /// <summary>
    /// Send import message to message queue for async processing
    /// </summary>
    /// <param name="message">Import message containing CSV content and user info</param>
    /// <returns>Message ID for tracking</returns>
    Task<string> SendImportMessageAsync(ImportMessage message);
}