using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TodoApp.Domain.Entities;
using TodoApp.Infrastructure.Data;
using TodoApp.Infrastructure.Repositories;

namespace TodoApp.Tests.Infrastructure;

public class TodoRepositoryTests : IDisposable
{
    private readonly TodoDbContext _context;
    private readonly TodoRepository _repository;

    public TodoRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<TodoDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TodoDbContext(options);
        _repository = new TodoRepository(_context);
    }

    [Fact]
    public async Task CreateAsync_WithValidTodoItem_ShouldAddToDatabase()
    {
        // Arrange
        var todoItem = TodoItem.Create("Test Todo", "Test Description", Priority.Medium, Category.Work);

        // Act
        var result = await _repository.CreateAsync(todoItem);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Title.Should().Be("Test Todo");
        
        var dbItem = await _context.TodoItems.FirstOrDefaultAsync(t => t.Id == result.Id);
        dbItem.Should().NotBeNull();
        dbItem!.Title.Should().Be("Test Todo");
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ShouldReturnTodoItem()
    {
        // Arrange
        var todoItem = TodoItem.Create("Test Todo", "Test Description", Priority.High, Category.Personal);
        _context.TodoItems.Add(todoItem);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(todoItem.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(todoItem.Id);
        result.Title.Should().Be("Test Todo");
        result.Description.Should().Be("Test Description");
        result.Priority.Should().Be(Priority.High);
        result.Category.Should().Be(Category.Personal);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ShouldReturnNull()
    {
        // Arrange
        var nonExistingId = 999L;

        // Act
        var result = await _repository.GetByIdAsync(nonExistingId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllTodoItems()
    {
        // Arrange
        var todoItems = new List<TodoItem>
        {
            TodoItem.Create("Todo 1", "Description 1", Priority.Low, Category.General),
            TodoItem.Create("Todo 2", "Description 2", Priority.Medium, Category.Work),
            TodoItem.Create("Todo 3", "Description 3", Priority.High, Category.Personal)
        };

        _context.TodoItems.AddRange(todoItems);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(t => t.Title == "Todo 1");
        result.Should().Contain(t => t.Title == "Todo 2");
        result.Should().Contain(t => t.Title == "Todo 3");
    }

    [Fact]
    public async Task GetCompletedTodosAsync_ShouldReturnOnlyCompletedTodos()
    {
        // Arrange
        var completedTodo = TodoItem.Create("Completed Todo", "Description", Priority.Medium, Category.Work);
        completedTodo.MarkAsCompleted();

        var incompleteTodo = TodoItem.Create("Incomplete Todo", "Description", Priority.Medium, Category.Work);

        _context.TodoItems.AddRange(completedTodo, incompleteTodo);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetCompletedTodosAsync();

        // Assert
        result.Should().ContainSingle();
        result.First().Title.Should().Be("Completed Todo");
        result.First().IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task GetIncompleteTodosAsync_ShouldReturnOnlyIncompleteTodos()
    {
        // Arrange
        var completedTodo = TodoItem.Create("Completed Todo", "Description", Priority.Medium, Category.Work);
        completedTodo.MarkAsCompleted();

        var incompleteTodo = TodoItem.Create("Incomplete Todo", "Description", Priority.Medium, Category.Work);

        _context.TodoItems.AddRange(completedTodo, incompleteTodo);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetIncompleteTodosAsync();

        // Assert
        result.Should().ContainSingle();
        result.First().Title.Should().Be("Incomplete Todo");
        result.First().IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_WithExistingTodoItem_ShouldUpdateInDatabase()
    {
        // Arrange
        var todoItem = TodoItem.Create("Original Title", "Original Description", Priority.Low, Category.General);
        _context.TodoItems.Add(todoItem);
        await _context.SaveChangesAsync();

        // Modify the item
        todoItem.UpdateTitle("Updated Title");
        todoItem.UpdateDescription("Updated Description");
        todoItem.SetPriority(Priority.High);

        // Act
        var result = await _repository.UpdateAsync(todoItem);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Updated Title");
        result.Description.Should().Be("Updated Description");
        result.Priority.Should().Be(Priority.High);

        // Verify in database
        var dbItem = await _context.TodoItems.FirstOrDefaultAsync(t => t.Id == todoItem.Id);
        dbItem!.Title.Should().Be("Updated Title");
        dbItem.Description.Should().Be("Updated Description");
        dbItem.Priority.Should().Be(Priority.High);
    }

    [Fact]
    public async Task DeleteAsync_WithExistingId_ShouldRemoveFromDatabaseAndReturnTrue()
    {
        // Arrange
        var todoItem = TodoItem.Create("To Delete", "Description", Priority.Medium, Category.Work);
        _context.TodoItems.Add(todoItem);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.DeleteAsync(todoItem.Id);

        // Assert
        result.Should().BeTrue();

        var dbItem = await _context.TodoItems.FirstOrDefaultAsync(t => t.Id == todoItem.Id);
        dbItem.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistingId_ShouldReturnFalse()
    {
        // Arrange
        var nonExistingId = 999L;

        // Act
        var result = await _repository.DeleteAsync(nonExistingId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_WithExistingId_ShouldReturnTrue()
    {
        // Arrange
        var todoItem = TodoItem.Create("Existing Todo", "Description", Priority.Medium, Category.Work);
        _context.TodoItems.Add(todoItem);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.ExistsAsync(todoItem.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistingId_ShouldReturnFalse()
    {
        // Arrange
        var nonExistingId = 999L;

        // Act
        var result = await _repository.ExistsAsync(nonExistingId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SearchTodosAsync_ShouldReturnMatchingTodos()
    {
        // Arrange
        var todoItems = new List<TodoItem>
        {
            TodoItem.Create("Buy groceries", "Need milk and eggs", Priority.Medium, Category.Personal),
            TodoItem.Create("Meeting with client", "Discuss project requirements", Priority.High, Category.Work),
            TodoItem.Create("Grocery shopping", "Weekly shopping", Priority.Low, Category.Personal)
        };

        _context.TodoItems.AddRange(todoItems);
        await _context.SaveChangesAsync();

        // Act - search for "grocer" which should match both "Buy groceries" and "Grocery shopping"
        var result = await _repository.SearchTodosAsync("grocer");

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(t => t.Title == "Buy groceries");
        result.Should().Contain(t => t.Title == "Grocery shopping");
    }

    [Fact]
    public async Task GetTodosByTagsAsync_ShouldReturnTodosWithMatchingTags()
    {
        // Arrange
        var todo1 = TodoItem.Create("Todo 1", "Description", Priority.Medium, Category.Work);
        todo1.SetTags("urgent,work");

        var todo2 = TodoItem.Create("Todo 2", "Description", Priority.Medium, Category.Personal);
        todo2.SetTags("personal,urgent");

        var todo3 = TodoItem.Create("Todo 3", "Description", Priority.Medium, Category.Work);
        todo3.SetTags("work,later");

        _context.TodoItems.AddRange(todo1, todo2, todo3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetTodosByTagsAsync("urgent");

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(t => t.Title == "Todo 1");
        result.Should().Contain(t => t.Title == "Todo 2");
    }

    [Fact]
    public async Task GetOverdueTodosAsync_ShouldReturnOverdueTodos()
    {
        // Arrange
        var overdueTodo = TodoItem.Create("Overdue Todo", "Description", Priority.High, Category.Work);
        // Set past due date directly using property
        typeof(TodoItem).GetProperty("DueDate")?.SetValue(overdueTodo, DateTime.UtcNow.AddDays(-1));
        
        var futureTodo = TodoItem.Create("Future Todo", "Description", Priority.Medium, Category.Personal);
        futureTodo.SetDueDate(DateTime.UtcNow.AddDays(2));

        var completedOverdueTodo = TodoItem.Create("Completed Overdue", "Description", Priority.Low, Category.General);
        typeof(TodoItem).GetProperty("DueDate")?.SetValue(completedOverdueTodo, DateTime.UtcNow.AddDays(-1));
        completedOverdueTodo.MarkAsCompleted();

        _context.TodoItems.AddRange(overdueTodo, futureTodo, completedOverdueTodo);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetOverdueTodosAsync();

        // Assert
        result.Should().ContainSingle();
        result.First().Title.Should().Be("Overdue Todo");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}