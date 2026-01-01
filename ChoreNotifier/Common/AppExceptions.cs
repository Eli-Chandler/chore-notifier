using Microsoft.AspNetCore.Diagnostics;
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