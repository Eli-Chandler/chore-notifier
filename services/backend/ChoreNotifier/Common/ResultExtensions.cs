using ChoreNotifier.Models;
using FluentResults;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ChoreNotifier.Common;

public static class ResultExtensions
{
    public static Results<Ok<T>, ProblemHttpResult> ToResponse<T>(this Result<T> result)
    {
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : TypedResults.Problem(CreateProblemDetails(result.Errors));
    }

    public static Results<NoContent, ProblemHttpResult> ToResponse(this Result result)
    {
        return result.IsSuccess
            ? TypedResults.NoContent()
            : TypedResults.Problem(CreateProblemDetails(result.Errors));
    }

    public static Results<Created<T>, ProblemHttpResult> ToCreatedResponse<T>(this Result<T> result, Func<T, string> uriFactory)
    {
        return result.IsSuccess
            ? TypedResults.Created(uriFactory(result.Value), result.Value)
            : TypedResults.Problem(CreateProblemDetails(result.Errors));
    }


    private static ProblemDetails CreateProblemDetails(IEnumerable<IError> errors)
    {
        var errorsList = errors.ToList();
        var firstError = errorsList.FirstOrDefault();
        var (statusCode, title) = GetStatusCodeAndTitle(firstError);

        var errorDetails = errorsList.Select(e => new
        {
            message = e.Message,
            code = e.Metadata.TryGetValue("code", out var code) ? code?.ToString() : null,
            metadata = e.Metadata.Where(m => m.Key != "code").ToDictionary(m => m.Key, m => m.Value)
        }).ToArray();

        return new ProblemDetails
        {
            Title = title,
            Detail = string.Join("; ", errorsList.Select(e => e.Message)),
            Status = statusCode,
            Extensions =
            {
                ["errors"] = errorDetails
            }
        };
    }

    private static (int statusCode, string title) GetStatusCodeAndTitle(IError? error)
    {
        return error switch
        {
            NotFoundError => (StatusCodes.Status404NotFound, "Resource Not Found"),
            ConflictError => (StatusCodes.Status409Conflict, "Conflict"),
            ValidationError => (StatusCodes.Status400BadRequest, "Validation Failed"),
            ForbiddenError => (StatusCodes.Status403Forbidden, "Forbidden"),
            InvalidOperationError => (StatusCodes.Status422UnprocessableEntity, "Invalid Operation"),
            _ => (StatusCodes.Status400BadRequest, "Bad Request")
        };
    }

    /// <summary>
    /// Adds all possible problem detail responses to the endpoint for OpenAPI documentation
    /// </summary>
    public static RouteHandlerBuilder WithProblemDetails(this RouteHandlerBuilder builder)
    {
        return builder
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }
}
