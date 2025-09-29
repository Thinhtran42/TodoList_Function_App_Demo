using FluentAssertions;
using TodoApp.Application.DTOs;
using TodoApp.Application.Validators;
using TodoApp.Domain.Entities;

namespace TodoApp.Tests.Application;

public class UpdateTodoRequestValidatorTests
{
    private readonly UpdateTodoRequestValidator _validator;

    public UpdateTodoRequestValidatorTests()
    {
        _validator = new UpdateTodoRequestValidator();
    }

    [Fact]
    public void Validate_WithValidRequest_ShouldPassValidation()
    {
        // Arrange
        var request = new UpdateTodoRequest
        {
            Title = "Updated Title",
            Description = "Updated Description",
            IsCompleted = true,
            Priority = Priority.High,
            Category = Category.Personal,
            DueDate = DateTime.UtcNow.AddDays(2),
            Tags = "updated,tags"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithEmptyTitle_ShouldFailValidation()
    {
        // Arrange
        var request = new UpdateTodoRequest
        {
            Title = "", // Invalid - empty but not null
            Description = "Valid Description",
            Priority = Priority.Medium,
            Category = Category.Work
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(UpdateTodoRequest.Title));
        result.Errors.First(e => e.PropertyName == nameof(UpdateTodoRequest.Title))
              .ErrorMessage.Should().Be("Title cannot be empty");
    }

    [Fact]
    public void Validate_WithNullTitle_ShouldPassValidation()
    {
        // Arrange - null title means "don't update this field"
        var request = new UpdateTodoRequest
        {
            Title = null,
            Description = "Valid Description",
            Priority = Priority.Medium,
            Category = Category.Work
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithTitleTooLong_ShouldFailValidation()
    {
        // Arrange
        var request = new UpdateTodoRequest
        {
            Title = new string('a', 501), // Exceeds 500 characters
            Description = "Valid Description"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(UpdateTodoRequest.Title));
        result.Errors.First(e => e.PropertyName == nameof(UpdateTodoRequest.Title))
              .ErrorMessage.Should().Be("Title cannot exceed 500 characters");
    }

    [Fact]
    public void Validate_WithDescriptionTooLong_ShouldFailValidation()
    {
        // Arrange
        var request = new UpdateTodoRequest
        {
            Title = "Valid Title",
            Description = new string('a', 2001) // Exceeds 2000 characters
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(UpdateTodoRequest.Description));
        result.Errors.First(e => e.PropertyName == nameof(UpdateTodoRequest.Description))
              .ErrorMessage.Should().Be("Description cannot exceed 2000 characters");
    }

    [Fact]
    public void Validate_WithInvalidPriority_ShouldFailValidation()
    {
        // Arrange
        var request = new UpdateTodoRequest
        {
            Title = "Valid Title",
            Priority = (Priority)999 // Invalid enum value
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(UpdateTodoRequest.Priority));
        result.Errors.First(e => e.PropertyName == nameof(UpdateTodoRequest.Priority))
              .ErrorMessage.Should().Be("Invalid priority value");
    }

    [Fact]
    public void Validate_WithNullPriority_ShouldPassValidation()
    {
        // Arrange - null priority means "don't update this field"
        var request = new UpdateTodoRequest
        {
            Title = "Valid Title",
            Priority = null
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithInvalidCategory_ShouldFailValidation()
    {
        // Arrange
        var request = new UpdateTodoRequest
        {
            Title = "Valid Title",
            Category = (Category)999 // Invalid enum value
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(UpdateTodoRequest.Category));
        result.Errors.First(e => e.PropertyName == nameof(UpdateTodoRequest.Category))
              .ErrorMessage.Should().Be("Invalid category value");
    }

    [Fact]
    public void Validate_WithPastDueDate_ShouldFailValidation()
    {
        // Arrange
        var request = new UpdateTodoRequest
        {
            Title = "Valid Title",
            DueDate = DateTime.UtcNow.AddDays(-1) // Past date
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(UpdateTodoRequest.DueDate));
        result.Errors.First(e => e.PropertyName == nameof(UpdateTodoRequest.DueDate))
              .ErrorMessage.Should().Be("Due date cannot be in the past");
    }

    [Fact]
    public void Validate_WithTagsTooLong_ShouldFailValidation()
    {
        // Arrange
        var request = new UpdateTodoRequest
        {
            Title = "Valid Title",
            Tags = new string('a', 501) // Exceeds 500 characters
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(UpdateTodoRequest.Tags));
        result.Errors.First(e => e.PropertyName == nameof(UpdateTodoRequest.Tags))
              .ErrorMessage.Should().Be("Tags cannot exceed 500 characters");
    }

    [Fact]
    public void Validate_WithAllNullValues_ShouldPassValidation()
    {
        // Arrange - all null means "don't update any field"
        var request = new UpdateTodoRequest
        {
            Title = null,
            Description = null,
            IsCompleted = null,
            Priority = null,
            Category = null,
            DueDate = null,
            Tags = null
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithMultipleErrors_ShouldReturnAllErrors()
    {
        // Arrange
        var request = new UpdateTodoRequest
        {
            Title = "", // Invalid - empty (not null)
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
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateTodoRequest.Title));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateTodoRequest.Description));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateTodoRequest.Priority));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateTodoRequest.Category));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateTodoRequest.DueDate));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateTodoRequest.Tags));
    }

    [Fact]
    public void Validate_WithValidBooleanValues_ShouldPassValidation()
    {
        // Arrange
        var request = new UpdateTodoRequest
        {
            Title = "Valid Title",
            IsCompleted = true // Valid boolean value
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithPartialUpdate_ShouldPassValidation()
    {
        // Arrange - only updating some fields
        var request = new UpdateTodoRequest
        {
            Title = "New Title",
            IsCompleted = true,
            // Other fields are null - means don't update them
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}