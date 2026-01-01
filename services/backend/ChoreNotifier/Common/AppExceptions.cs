using Microsoft.AspNetCore.Diagnostics;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace ChoreNotifier.Common;

public abstract class AppException : Exception
{
    protected AppException(string message) : base(message)
    {
    }
}

public sealed class NotFoundException : AppException
{
    public string Entity { get; }
    public object Key { get; }
    
    public NotFoundException(string entity, object key)
        : base($"{entity} with key '{key}' was not found.")
    {
        Entity = entity;
        Key = key;
    }
}

public sealed class AppExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is NotFoundException nf)
        {
            httpContext.Response.StatusCode = StatusCodes.Status404NotFound;

            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Not Found",
                Detail = nf.Message
            };

            await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
            return true;
        }

        return false;
    }
}

public sealed class ValidationExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ValidationException fv)
            return false;

        var errors = fv.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray()
            );

        var problem = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
        };

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

        return true;
    }
}