using System;
using TodoApp.Application.DTOs;

namespace TodoApp.Application.Interfaces.Services.MessageBroker;

/// <summary>
/// Interface for message queue operations (Azure Service Bus, RabbitMQ, etc.)
/// </summary>
public interface IMessageQueueService
{
    /// <summary>
    /// Send a message to the message queue for async processing
    /// </summary>
    /// <typeparam name="T">Message type - must be a class</typeparam>
    /// <param name="message">Message object to send</param>
    /// <param name="routingKey">Optional routing key for topic-based routing. If null, uses default from settings.</param>
    /// <returns>Message ID for tracking</returns>
    Task<string> SendMessageAsync<T>(T message, string? routingKey = null) where T : class;
}