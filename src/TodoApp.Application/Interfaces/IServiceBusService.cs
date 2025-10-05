using TodoApp.Application.DTOs;

namespace TodoApp.Application.Interfaces;

public interface IServiceBusService
{
    /// <summary>
    /// Send import message to ServiceBus queue for async processing
    /// </summary>
    /// <param name="message">Import message containing CSV content and user info</param>
    /// <returns>Message ID for tracking</returns>
    Task<string> SendImportMessageAsync(ImportMessage message);
}