using FluentAssertions;
using TodoApp.Application.DTOs;
using TodoApp.Application.Validators;
using TodoApp.Domain.Entities;

namespace TodoApp.Tests.Application;

public class CreateTodoRequestValidatorTests
{
    private readonly CreateTodoRequestValidator _validator;

    public CreateTodoRequestValidatorTests()
    {
        _validator = new CreateTodoRequestValidator();
    }

    [Fact]
    public void Validate_WithValidRequest_ShouldPassValidation()
    {
        // Arrange
        var request = new CreateTodoRequest
        {
            Title = "Valid Title",
            Description = "Valid Description",
            Priority = Priority.Medium,
            Category = Category.Work,
            DueDate = DateTime.UtcNow.AddDays(1),
            Tags = "tag1,tag2"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithInvalidTitle_ShouldFailValidation(string? invalidTitle)
    {
        // Arrange
        var request = new CreateTodoRequest
        {
            Title = invalidTitle!,
            Description = "Valid Description",
            Priority = Priority.Medium,
            Category = Category.Work
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(CreateTodoRequest.Title));
        result.Errors.First(e => e.PropertyName == nameof(CreateTodoRequest.Title))
              .ErrorMessage.Should().Be("Title is required");
    }

    [Fact]
    public void Validate_WithTitleTooLong_ShouldFailValidation()
    {
        // Arrange
        var request = new CreateTodoRequest
        {
            Title = new string('a', 501), // Exceeds 500 characters
            Description = "Valid Description",
            Priority = Priority.Medium,
            Category = Category.Work
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(CreateTodoRequest.Title));
        result.Errors.First(e => e.PropertyName == nameof(CreateTodoRequest.Title))
              .ErrorMessage.Should().Be("Title cannot exceed 500 characters");
    }

    [Fact]
    public void Validate_WithDescriptionTooLong_ShouldFailValidation()
    {
        // Arrange
        var request = new CreateTodoRequest
        {
            Title = "Valid Title",
            Description = new string('a', 2001), // Exceeds 2000 characters
            Priority = Priority.Medium,
            Category = Category.Work
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(CreateTodoRequest.Description));
        result.Errors.First(e => e.PropertyName == nameof(CreateTodoRequest.Description))
              .ErrorMessage.Should().Be("Description cannot exceed 2000 characters");
    }

    [Fact]
    public void Validate_WithInvalidPriority_ShouldFailValidation()
    {
        // Arrange
        var request = new CreateTodoRequest
        {
            Title = "Valid Title",
            Description = "Valid Description",
            Priority = (Priority)999, // Invalid enum value
            Category = Category.Work
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(CreateTodoRequest.Priority));
        result.Errors.First(e => e.PropertyName == nameof(CreateTodoRequest.Priority))
              .ErrorMessage.Should().Be("Invalid priority value");
    }

    [Fact]
    public void Validate_WithInvalidCategory_ShouldFailValidation()
    {
        // Arrange
        var request = new CreateTodoRequest
        {
            Title = "Valid Title",
            Description = "Valid Description",
            Priority = Priority.Medium,
            Category = (Category)999 // Invalid enum value
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(CreateTodoRequest.Category));
        result.Errors.First(e => e.PropertyName == nameof(CreateTodoRequest.Category))
              .ErrorMessage.Should().Be("Invalid category value");
    }

    [Fact]
    public void Validate_WithPastDueDate_ShouldFailValidation()
    {
        // Arrange
        var request = new CreateTodoRequest
        {
            Title = "Valid Title",
            Description = "Valid Description",
            Priority = Priority.Medium,
            Category = Category.Work,
            DueDate = DateTime.UtcNow.AddDays(-1) // Past date
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(CreateTodoRequest.DueDate));
        result.Errors.First(e => e.PropertyName == nameof(CreateTodoRequest.DueDate))
              .ErrorMessage.Should().Be("Due date cannot be in the past");
    }

    [Fact]
    public void Validate_WithTagsTooLong_ShouldFailValidation()
    {
        // Arrange
        var request = new CreateTodoRequest
        {
            Title = "Valid Title",
            Description = "Valid Description",
            Priority = Priority.Medium,
            Category = Category.Work,
            Tags = new string('a', 501) // Exceeds 500 characters
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(CreateTodoRequest.Tags));
        result.Errors.First(e => e.PropertyName == nameof(CreateTodoRequest.Tags))
              .ErrorMessage.Should().Be("Tags cannot exceed 500 characters");
    }

    [Fact]
    public void Validate_WithMultipleErrors_ShouldReturnAllErrors()
    {
        // Arrange
        var request = new CreateTodoRequest
        {
            Title = "", // Invalid - empty
            Description = new string('a', 2001), // Invalid - too long
            Priority = (Priority)999, // Invalid enum
            Category = (Category)999, // Invalid enum
            DueDate = DateTime.UtcNow.AddDays(-1), // Invalid - past date
            Tags = new string('t', 501) // Invalid - too long
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(6); // All validation errors
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateTodoRequest.Title));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateTodoRequest.Description));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateTodoRequest.Priority));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateTodoRequest.Category));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateTodoRequest.DueDate));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateTodoRequest.Tags));
    }

    [Fact]
    public void Validate_WithNullOptionalFields_ShouldPassValidation()
    {
        // Arrange
        var request = new CreateTodoRequest
        {
            Title = "Valid Title",
            Description = null, // Optional
            Priority = Priority.Medium,
            Category = Category.Work,
            DueDate = null, // Optional
            Tags = null // Optional
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}