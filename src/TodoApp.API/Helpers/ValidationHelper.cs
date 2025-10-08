using FluentValidation;
using FluentValidation.Results;

namespace TodoApp.API.Helpers;

/// <summary>
/// Helper methods for FluentValidation in ASP.NET Core Web API
/// </summary>
public static class ValidationHelper
{
    /// <summary>
    /// Validate a model and return validation result
    /// </summary>
    public static async Task<ValidationResult> ValidateAsync<T>(
        IValidator<T> validator,
        T model)
    {
        return await validator.ValidateAsync(model);
    }

    /// <summary>
    /// Validate a model and return (IsValid, ErrorDetails)
    /// </summary>
    public static async Task<(bool IsValid, object? Errors)> ValidateAndGetErrorsAsync<T>(
        IValidator<T> validator,
        T model)
    {
        var validationResult = await validator.ValidateAsync(model);

        if (!validationResult.IsValid)
        {
            var errors = new
            {
                error = "Validation failed",
                details = validationResult.Errors.Select(e => new
                {
                    field = e.PropertyName,
                    message = e.ErrorMessage
                })
            };
            return (false, errors);
        }

        return (true, null);
    }

    /// <summary>
    /// Format validation errors for API response
    /// </summary>
    public static object FormatValidationErrors(ValidationResult validationResult)
    {
        return new
        {
            error = "Validation failed",
            details = validationResult.Errors.Select(e => new
            {
                field = e.PropertyName,
                message = e.ErrorMessage,
                code = e.ErrorCode
            })
        };
    }

    /// <summary>
    /// Get validation errors as dictionary (field -> messages[])
    /// </summary>
    public static Dictionary<string, string[]> GetErrorsDictionary(ValidationResult validationResult)
    {
        return validationResult.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray()
            );
    }
}