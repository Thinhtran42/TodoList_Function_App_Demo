using FluentAssertions;
using TodoApp.Domain.Entities;
using TodoApp.Domain.Exceptions;

namespace TodoApp.Tests.Domain;

public class TodoItemTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldCreateTodoItem()
    {
        // Arrange
        var title = "Test Todo";
        var description = "Test Description";
        var priority = Priority.High;
        var category = Category.Work;

        // Act
        var todoItem = TodoItem.Create(title, description, priority, category);

        // Assert
        todoItem.Should().NotBeNull();
        todoItem.Title.Should().Be(title);
        todoItem.Description.Should().Be(description);
        todoItem.Priority.Should().Be(priority);
        todoItem.Category.Should().Be(category);
        todoItem.IsCompleted.Should().BeFalse();
        todoItem.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        todoItem.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithInvalidTitle_ShouldThrowArgumentException(string? invalidTitle)
    {
        // Act & Assert
        var act = () => TodoItem.Create(invalidTitle!, "Description", Priority.Medium, Category.General);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Title*");
    }

    [Fact]
    public void Create_WithTitleTooLong_ShouldThrowArgumentException()
    {
        // Arrange
        var longTitle = new string('a', 501);

        // Act & Assert
        var act = () => TodoItem.Create(longTitle, "Description", Priority.Medium, Category.General);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Title*500*");
    }

    [Fact]
    public void UpdateTitle_WithValidTitle_ShouldUpdateTitle()
    {
        // Arrange
        var todoItem = TodoItem.Create("Original", "Description", Priority.Medium, Category.General);
        var newTitle = "Updated Title";
        var originalUpdatedAt = todoItem.UpdatedAt;

        Thread.Sleep(1); // Ensure different timestamp

        // Act
        todoItem.UpdateTitle(newTitle);

        // Assert
        todoItem.Title.Should().Be(newTitle);
        todoItem.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void UpdateTitle_WithInvalidTitle_ShouldThrowArgumentException(string? invalidTitle)
    {
        // Arrange
        var todoItem = TodoItem.Create("Original", "Description", Priority.Medium, Category.General);

        // Act & Assert
        var act = () => todoItem.UpdateTitle(invalidTitle!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateDescription_WithValidDescription_ShouldUpdateDescription()
    {
        // Arrange
        var todoItem = TodoItem.Create("Title", "Original", Priority.Medium, Category.General);
        var newDescription = "Updated Description";
        var originalUpdatedAt = todoItem.UpdatedAt;

        Thread.Sleep(1);

        // Act
        todoItem.UpdateDescription(newDescription);

        // Assert
        todoItem.Description.Should().Be(newDescription);
        todoItem.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void UpdateDescription_WithDescriptionTooLong_ShouldThrowArgumentException()
    {
        // Arrange
        var todoItem = TodoItem.Create("Title", "Original", Priority.Medium, Category.General);
        var longDescription = new string('a', 2001);

        // Act & Assert
        var act = () => todoItem.UpdateDescription(longDescription);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Description*2000*");
    }

    [Fact]
    public void MarkAsCompleted_ShouldSetIsCompletedToTrue()
    {
        // Arrange
        var todoItem = TodoItem.Create("Title", "Description", Priority.Medium, Category.General);
        var originalUpdatedAt = todoItem.UpdatedAt;

        Thread.Sleep(1);

        // Act
        todoItem.MarkAsCompleted();

        // Assert
        todoItem.IsCompleted.Should().BeTrue();
        todoItem.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void MarkAsIncomplete_ShouldSetIsCompletedToFalse()
    {
        // Arrange
        var todoItem = TodoItem.Create("Title", "Description", Priority.Medium, Category.General);
        todoItem.MarkAsCompleted();
        var originalUpdatedAt = todoItem.UpdatedAt;

        Thread.Sleep(1);

        // Act
        todoItem.MarkAsIncomplete();

        // Assert
        todoItem.IsCompleted.Should().BeFalse();
        todoItem.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void SetPriority_ShouldUpdatePriority()
    {
        // Arrange
        var todoItem = TodoItem.Create("Title", "Description", Priority.Low, Category.General);
        var originalUpdatedAt = todoItem.UpdatedAt;

        Thread.Sleep(1);

        // Act
        todoItem.SetPriority(Priority.Critical);

        // Assert
        todoItem.Priority.Should().Be(Priority.Critical);
        todoItem.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void SetCategory_ShouldUpdateCategory()
    {
        // Arrange
        var todoItem = TodoItem.Create("Title", "Description", Priority.Medium, Category.General);
        var originalUpdatedAt = todoItem.UpdatedAt;

        Thread.Sleep(1);

        // Act
        todoItem.SetCategory(Category.Personal);

        // Assert
        todoItem.Category.Should().Be(Category.Personal);
        todoItem.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void SetDueDate_WithValidDate_ShouldUpdateDueDate()
    {
        // Arrange
        var todoItem = TodoItem.Create("Title", "Description", Priority.Medium, Category.General);
        var dueDate = DateTime.UtcNow.AddDays(1);
        var originalUpdatedAt = todoItem.UpdatedAt;

        Thread.Sleep(1);

        // Act
        todoItem.SetDueDate(dueDate);

        // Assert
        todoItem.DueDate.Should().Be(dueDate);
        todoItem.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void SetDueDate_WithPastDate_ShouldThrowArgumentException()
    {
        // Arrange
        var todoItem = TodoItem.Create("Title", "Description", Priority.Medium, Category.General);
        var pastDate = DateTime.UtcNow.AddDays(-1);

        // Act & Assert
        var act = () => todoItem.SetDueDate(pastDate);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*past*");
    }

    [Fact]
    public void SetTags_WithValidTags_ShouldUpdateTags()
    {
        // Arrange
        var todoItem = TodoItem.Create("Title", "Description", Priority.Medium, Category.General);
        var tags = "tag1,tag2,tag3";
        var originalUpdatedAt = todoItem.UpdatedAt;

        Thread.Sleep(1);

        // Act
        todoItem.SetTags(tags);

        // Assert
        todoItem.Tags.Should().Be(tags);
        todoItem.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void SetTags_WithTagsTooLong_ShouldThrowArgumentException()
    {
        // Arrange
        var todoItem = TodoItem.Create("Title", "Description", Priority.Medium, Category.General);
        var longTags = new string('a', 501);

        // Act & Assert
        var act = () => todoItem.SetTags(longTags);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Tags*500*");
    }

    [Fact]
    public void IsOverdue_WhenDueDateIsInPastAndNotCompleted_ShouldReturnTrue()
    {
        // Arrange
        var todoItem = TodoItem.Create("Title", "Description", Priority.Medium, Category.General);
        // Set due date to past directly using property
        typeof(TodoItem).GetProperty("DueDate")?.SetValue(todoItem, DateTime.UtcNow.AddDays(-1));

        // Act
        var isOverdue = todoItem.IsOverdue();

        // Assert
        isOverdue.Should().BeTrue();
    }

    [Fact]
    public void IsOverdue_WhenCompleted_ShouldReturnFalse()
    {
        // Arrange
        var todoItem = TodoItem.Create("Title", "Description", Priority.Medium, Category.General);
        typeof(TodoItem).GetProperty("DueDate")?.SetValue(todoItem, DateTime.UtcNow.AddDays(-1));
        todoItem.MarkAsCompleted();

        // Act
        var isOverdue = todoItem.IsOverdue();

        // Assert
        isOverdue.Should().BeFalse();
    }

    [Fact]
    public void IsOverdue_WhenNoDueDate_ShouldReturnFalse()
    {
        // Arrange
        var todoItem = TodoItem.Create("Title", "Description", Priority.Medium, Category.General);

        // Act
        var isOverdue = todoItem.IsOverdue();

        // Assert
        isOverdue.Should().BeFalse();
    }

    [Fact]
    public void IsOverdue_WhenDueDateIsInFuture_ShouldReturnFalse()
    {
        // Arrange
        var todoItem = TodoItem.Create("Title", "Description", Priority.Medium, Category.General);
        todoItem.SetDueDate(DateTime.UtcNow.AddDays(1));

        // Act
        var isOverdue = todoItem.IsOverdue();

        // Assert
        isOverdue.Should().BeFalse();
    }
}