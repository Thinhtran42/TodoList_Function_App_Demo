namespace TodoApp.Domain.Exceptions;

public class TodoNotFoundException : Exception
{
    public TodoNotFoundException(long id) 
        : base($"Todo with ID {id} was not found")
    {
        TodoId = id;
    }

    public TodoNotFoundException(string message) 
        : base(message)
    {
        TodoId = 0;
    }

    public long TodoId { get; }
}

public class TodoValidationException : Exception
{
    public TodoValidationException(string message) : base(message)
    {
    }

    public TodoValidationException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}