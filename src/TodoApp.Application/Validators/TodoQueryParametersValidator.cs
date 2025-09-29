using FluentValidation;
using TodoApp.Application.DTOs;

namespace TodoApp.Application.Validators;

public class TodoQueryParametersValidator : AbstractValidator<TodoQueryParameters>
{
    public TodoQueryParametersValidator()
    {
        RuleFor(x => x.Priority)
            .IsInEnum()
            .WithMessage("Invalid priority value")
            .When(x => x.Priority.HasValue);

        RuleFor(x => x.Category)
            .IsInEnum()
            .WithMessage("Invalid category value")
            .When(x => x.Category.HasValue);

        RuleFor(x => x.DueDateFrom)
            .LessThanOrEqualTo(x => x.DueDateTo)
            .WithMessage("DueDateFrom must be less than or equal to DueDateTo")
            .When(x => x.DueDateFrom.HasValue && x.DueDateTo.HasValue);

        RuleFor(x => x.SearchTerm)
            .MaximumLength(200)
            .WithMessage("Search term cannot exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.SearchTerm));

        RuleFor(x => x.Tags)
            .MaximumLength(500)
            .WithMessage("Tags cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Tags));

        RuleFor(x => x.SortBy)
            .NotEmpty()
            .WithMessage("SortBy is required")
            .Must(BeValidSortField)
            .WithMessage("Invalid sort field. Valid fields are: Id, Title, CreatedAt, UpdatedAt, Priority, Category, DueDate");

        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("Page must be greater than 0");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Page size must be between 1 and 100");
    }

    private bool BeValidSortField(string sortBy)
    {
        var validFields = new[] { "Id", "Title", "CreatedAt", "UpdatedAt", "Priority", "Category", "DueDate", "IsCompleted" };
        return validFields.Contains(sortBy, StringComparer.OrdinalIgnoreCase);
    }
}