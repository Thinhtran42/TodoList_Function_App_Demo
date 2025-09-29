using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TodoApp.Application.DTOs;
using TodoApp.Application.Common;
using TodoApp.Application.Interfaces;
using TodoApp.Application.Services;
using TodoApp.Domain.Entities;
using TodoApp.Domain.Exceptions;

namespace TodoApp.Tests.Application;

public class TodoServiceTests
{
    private readonly Mock<ITodoRepository> _mockRepository;
    private readonly Mock<ILogger<TodoService>> _mockLogger;
    private readonly TodoService _todoService;

    public TodoServiceTests()
    {
        _mockRepository = new Mock<ITodoRepository>();
        _mockLogger = new Mock<ILogger<TodoService>>();
        _todoService = new TodoService(_mockRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task CreateTodoAsync_WithValidRequest_ShouldReturnTodoDto()
    {
        // Arrange
        var request = new CreateTodoRequest
        {
            Title = "Test Todo",
            Description = "Test Description",
            Priority = Priority.High,
            Category = Category.Work,
            DueDate = DateTime.UtcNow.AddDays(1),
            Tags = "tag1,tag2"
        };

        var createdTodo = TodoItem.Create(request.Title, request.Description, request.Priority, request.Category);
        createdTodo.SetDueDate(request.DueDate);
        createdTodo.SetTags(request.Tags);

        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<TodoItem>()))
                      .ReturnsAsync(createdTodo);

        // Act
        var result = await _todoService.CreateTodoAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be(request.Title);
        result.Description.Should().Be(request.Description);
        result.Priority.Should().Be(request.Priority);
        result.Category.Should().Be(request.Category);
        result.DueDate.Should().Be(request.DueDate);
        result.Tags.Should().Be(request.Tags);

        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<TodoItem>()), Times.Once);
    }

    [Fact]
    public async Task CreateTodoAsync_WithInvalidTitle_ShouldThrowTodoValidationException()
    {
        // Arrange
        var request = new CreateTodoRequest
        {
            Title = "", // Invalid title
            Description = "Test Description",
            Priority = Priority.Medium,
            Category = Category.General
        };

        // Act & Assert
        var act = async () => await _todoService.CreateTodoAsync(request);
        await act.Should().ThrowAsync<TodoValidationException>();

        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<TodoItem>()), Times.Never);
    }

    [Fact]
    public async Task GetTodoByIdAsync_WithExistingId_ShouldReturnTodoDto()
    {
        // Arrange
        var todoId = 1L;
        var todoItem = TodoItem.Create("Test Todo", "Test Description", Priority.Medium, Category.General);
        
        _mockRepository.Setup(r => r.GetByIdAsync(todoId))
                      .ReturnsAsync(todoItem);

        // Act
        var result = await _todoService.GetTodoByIdAsync(todoId);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be(todoItem.Title);
        result.Description.Should().Be(todoItem.Description);

        _mockRepository.Verify(r => r.GetByIdAsync(todoId), Times.Once);
    }

    [Fact]
    public async Task GetTodoByIdAsync_WithNonExistingId_ShouldThrowTodoNotFoundException()
    {
        // Arrange
        var todoId = 999L;
        _mockRepository.Setup(r => r.GetByIdAsync(todoId))
                      .ReturnsAsync((TodoItem?)null);

        // Act & Assert
        var act = async () => await _todoService.GetTodoByIdAsync(todoId);
        await act.Should().ThrowAsync<TodoNotFoundException>()
                .Where(ex => ex.TodoId == todoId);

        _mockRepository.Verify(r => r.GetByIdAsync(todoId), Times.Once);
    }

    [Fact]
    public async Task GetAllTodosAsync_ShouldReturnAllTodos()
    {
        // Arrange
        var todoItems = new List<TodoItem>
        {
            TodoItem.Create("Todo 1", "Description 1", Priority.Low, Category.Personal),
            TodoItem.Create("Todo 2", "Description 2", Priority.High, Category.Work)
        };

        _mockRepository.Setup(r => r.GetAllAsync())
                      .ReturnsAsync(todoItems);

        // Act
        var result = await _todoService.GetAllTodosAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(dto => dto.Should().NotBeNull());

        _mockRepository.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateTodoAsync_WithValidRequest_ShouldReturnUpdatedTodoDto()
    {
        // Arrange
        var todoId = 1L;
        var existingTodo = TodoItem.Create("Original Title", "Original Description", Priority.Low, Category.General);
        var updateRequest = new UpdateTodoRequest
        {
            Title = "Updated Title",
            Description = "Updated Description",
            Priority = Priority.High,
            Category = Category.Work,
            IsCompleted = true,
            DueDate = DateTime.UtcNow.AddDays(2),
            Tags = "updated,tags"
        };

        _mockRepository.Setup(r => r.GetByIdAsync(todoId))
                      .ReturnsAsync(existingTodo);

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<TodoItem>()))
                      .ReturnsAsync(existingTodo);

        // Act
        var result = await _todoService.UpdateTodoAsync(todoId, updateRequest);

        // Assert
        result.Should().NotBeNull();
        
        _mockRepository.Verify(r => r.GetByIdAsync(todoId), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<TodoItem>()), Times.Once);
    }

    [Fact]
    public async Task UpdateTodoAsync_WithNonExistingId_ShouldThrowTodoNotFoundException()
    {
        // Arrange
        var todoId = 999L;
        var updateRequest = new UpdateTodoRequest { Title = "Updated Title" };

        _mockRepository.Setup(r => r.GetByIdAsync(todoId))
                      .ReturnsAsync((TodoItem?)null);

        // Act & Assert
        var act = async () => await _todoService.UpdateTodoAsync(todoId, updateRequest);
        await act.Should().ThrowAsync<TodoNotFoundException>()
                .Where(ex => ex.TodoId == todoId);

        _mockRepository.Verify(r => r.GetByIdAsync(todoId), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<TodoItem>()), Times.Never);
    }

    [Fact]
    public async Task DeleteTodoAsync_WithExistingId_ShouldReturnTrue()
    {
        // Arrange
        var todoId = 1L;
        _mockRepository.Setup(r => r.DeleteAsync(todoId))
                      .ReturnsAsync(true);

        // Act
        var result = await _todoService.DeleteTodoAsync(todoId);

        // Assert
        result.Should().BeTrue();
        _mockRepository.Verify(r => r.DeleteAsync(todoId), Times.Once);
    }

    [Fact]
    public async Task DeleteTodoAsync_WithNonExistingId_ShouldReturnFalse()
    {
        // Arrange
        var todoId = 999L;
        _mockRepository.Setup(r => r.DeleteAsync(todoId))
                      .ReturnsAsync(false);

        // Act
        var result = await _todoService.DeleteTodoAsync(todoId);

        // Assert
        result.Should().BeFalse();
        _mockRepository.Verify(r => r.DeleteAsync(todoId), Times.Once);
    }

    [Fact]
    public async Task GetTodosAsync_WithValidParameters_ShouldReturnPagedResult()
    {
        // Arrange
        var parameters = new TodoQueryParameters
        {
            Page = 1,
            PageSize = 10,
            IsCompleted = false,
            Priority = Priority.High
        };

        var todoItems = new List<TodoItem>
        {
            TodoItem.Create("Todo 1", "Description 1", Priority.High, Category.Work),
            TodoItem.Create("Todo 2", "Description 2", Priority.High, Category.Personal)
        };

        var pagedResult = new PagedResult<TodoItem>
        {
            Items = todoItems,
            TotalItems = 2,
            Page = 1,
            PageSize = 10
            // TotalPages is computed automatically
        };

        _mockRepository.Setup(r => r.GetTodosAsync(parameters))
                      .ReturnsAsync(pagedResult);

        // Act
        var result = await _todoService.GetTodosAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalItems.Should().Be(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalPages.Should().Be(1);

        _mockRepository.Verify(r => r.GetTodosAsync(parameters), Times.Once);
    }

    [Fact]
    public async Task SearchTodosAsync_WithValidSearchTerm_ShouldReturnMatchingTodos()
    {
        // Arrange
        var searchTerm = "test";
        var todoItems = new List<TodoItem>
        {
            TodoItem.Create("Test Todo 1", "Test Description", Priority.Medium, Category.General),
            TodoItem.Create("Another test", "Different description", Priority.Low, Category.Personal)
        };

        _mockRepository.Setup(r => r.SearchTodosAsync(searchTerm))
                      .ReturnsAsync(todoItems);

        // Act
        var result = await _todoService.SearchTodosAsync(searchTerm);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(dto => 
            dto.Title.ToLower().Should().Contain(searchTerm.ToLower()));

        _mockRepository.Verify(r => r.SearchTodosAsync(searchTerm), Times.Once);
    }

    [Fact]
    public async Task GetOverdueTodosAsync_ShouldReturnOverdueTodos()
    {
        // Arrange
        var overdueTodos = new List<TodoItem>
        {
            TodoItem.Create("Overdue Todo 1", "Description", Priority.High, Category.Work),
            TodoItem.Create("Overdue Todo 2", "Description", Priority.Medium, Category.Personal)
        };

        _mockRepository.Setup(r => r.GetOverdueTodosAsync())
                      .ReturnsAsync(overdueTodos);

        // Act
        var result = await _todoService.GetOverdueTodosAsync();

        // Assert
        result.Should().HaveCount(2);
        _mockRepository.Verify(r => r.GetOverdueTodosAsync(), Times.Once);
    }

    [Fact]
    public async Task TodoExistsAsync_WithExistingId_ShouldReturnTrue()
    {
        // Arrange
        var todoId = 1L;
        _mockRepository.Setup(r => r.ExistsAsync(todoId))
                      .ReturnsAsync(true);

        // Act
        var result = await _todoService.TodoExistsAsync(todoId);

        // Assert
        result.Should().BeTrue();
        _mockRepository.Verify(r => r.ExistsAsync(todoId), Times.Once);
    }

    [Fact]
    public async Task TodoExistsAsync_WithNonExistingId_ShouldReturnFalse()
    {
        // Arrange
        var todoId = 999L;
        _mockRepository.Setup(r => r.ExistsAsync(todoId))
                      .ReturnsAsync(false);

        // Act
        var result = await _todoService.TodoExistsAsync(todoId);

        // Assert
        result.Should().BeFalse();
        _mockRepository.Verify(r => r.ExistsAsync(todoId), Times.Once);
    }
}