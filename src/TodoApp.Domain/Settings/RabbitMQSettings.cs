namespace TodoApp.Domain.Settings;

public class RabbitMQSettings
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string QueueName { get; set; } = "import-csv-queue";
    public string ExchangeName { get; set; } = "todo-exchange";
    public string RoutingKey { get; set; } = "todo.import";
}
