using FluentAssertions;
using TodoApp.Application.DTOs;
using TodoApp.Application.Mappers;
using TodoApp.Domain.Entities;

namespace TodoApp.Tests.Application;

public class TodoMapperTests
{
    [Fact]
    public void ToDto_WithValidTodoItem_ShouldMapCorrectly()
    {
        // Arrange
        var todoItem = TodoItem.Create("Test Title", "Test Description", Priority.High, Category.Work);
        todoItem.SetDueDate(DateTime.UtcNow.AddDays(1));
        todoItem.SetTags("tag1,tag2");
        todoItem.MarkAsCompleted();

        // Act
        var dto = TodoMapper.ToDto(todoItem);

        // Assert
        dto.Should().NotBeNull();
        dto.Id.Should().Be(todoItem.Id);
        dto.Title.Should().Be(todoItem.Title);
        dto.Description.Should().Be(todoItem.Description);
        dto.IsCompleted.Should().Be(todoItem.IsCompleted);
        dto.Priority.Should().Be(todoItem.Priority);
        dto.Category.Should().Be(todoItem.Category);
        dto.DueDate.Should().Be(todoItem.DueDate);
        dto.Tags.Should().Be(todoItem.Tags);
        dto.CreatedAt.Should().Be(todoItem.CreatedAt);
        dto.UpdatedAt.Should().Be(todoItem.UpdatedAt);
    }

    [Fact]
    public void ToDto_WithCollection_ShouldMapAllItems()
    {
        // Arrange
        var todoItems = new List<TodoItem>
        {
            TodoItem.Create("Title 1", "Description 1", Priority.Low, Category.Personal),
            TodoItem.Create("Title 2", "Description 2", Priority.Medium, Category.Work),
            TodoItem.Create("Title 3", "Description 3", Priority.High, Category.Health)
        };

        // Act
        var dtos = TodoMapper.ToDto(todoItems);

        // Assert
        dtos.Should().HaveCount(3);
        dtos.Should().AllSatisfy(dto => dto.Should().NotBeNull());
        
        var dtoList = dtos.ToList();
        for (int i = 0; i < todoItems.Count; i++)
        {
            dtoList[i].Title.Should().Be(todoItems[i].Title);
            dtoList[i].Description.Should().Be(todoItems[i].Description);
            dtoList[i].Priority.Should().Be(todoItems[i].Priority);
            dtoList[i].Category.Should().Be(todoItems[i].Category);
        }
    }

    [Fact]
    public void ToDto_WithEmptyCollection_ShouldReturnEmptyCollection()
    {
        // Arrange
        var todoItems = new List<TodoItem>();

        // Act
        var dtos = TodoMapper.ToDto(todoItems);

        // Assert
        dtos.Should().NotBeNull();
        dtos.Should().BeEmpty();
    }

    [Fact]
    public void ToDto_WithTodoItemWithNullOptionalFields_ShouldMapCorrectly()
    {
        // Arrange
        var todoItem = TodoItem.Create("Test Title", null, Priority.Medium, Category.General);

        // Act
        var dto = TodoMapper.ToDto(todoItem);

        // Assert
        dto.Should().NotBeNull();
        dto.Title.Should().Be(todoItem.Title);
        dto.Description.Should().BeNull();
        dto.DueDate.Should().BeNull();
        dto.Tags.Should().BeNull();
        dto.IsCompleted.Should().BeFalse();
    }
}