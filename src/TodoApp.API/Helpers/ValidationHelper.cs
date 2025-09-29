using FluentValidation;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace TodoApp.API.Helpers;

public static class ValidationHelper
{
    public static async Task<(bool IsValid, HttpResponseData? Response)> ValidateAsync<T>(
        IValidator<T> validator, 
        T model, 
        HttpRequestData req)
    {
        var validationResult = await validator.ValidateAsync(model);
        
        if (!validationResult.IsValid)
        {
            var response = req.CreateResponse(HttpStatusCode.BadRequest);
            await response.WriteAsJsonAsync(new
            {
                error = "Validation failed",
                details = validationResult.Errors.Select(e => new
                {
                    Field = e.PropertyName,
                    Message = e.ErrorMessage
                })
            });
            return (false, response);
        }
        
        return (true, null);
    }
}