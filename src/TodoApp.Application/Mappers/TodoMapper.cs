using TodoApp.Application.DTOs;
using TodoApp.Domain.Entities;

namespace TodoApp.Application.Mappers;

public static class TodoMapper
{
    public static TodoDto ToDto(TodoItem todoItem)
    {
        return new TodoDto
        {
            Id = todoItem.Id,
            Title = todoItem.Title,
            Description = todoItem.Description,
            IsCompleted = todoItem.IsCompleted,
            Priority = todoItem.Priority,
            Category = todoItem.Category,
            DueDate = todoItem.DueDate,
            Tags = todoItem.Tags,
            CreatedAt = todoItem.CreatedAt,
            UpdatedAt = todoItem.UpdatedAt
        };
    }

    public static IEnumerable<TodoDto> ToDto(IEnumerable<TodoItem> todoItems)
    {
        return todoItems.Select(ToDto);
    }
}