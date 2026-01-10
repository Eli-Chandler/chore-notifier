using ChoreNotifier.Models;
using FluentResults;

namespace ChoreNotifier.Common;

public static class ResultExtensions
{
    public static IResult ToResult<T>(this Result<T> result)
    {
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : CreateProblemDetails(result.Errors);
    }

    public static IResult ToResult(this Result result)
    {
        return result.IsSuccess
            ? Results.NoContent()
            : CreateProblemDetails(result.Errors);
    }

    public static IResult ToCreatedResult<T>(this Result<T> result, Func<T, string> uriFactory)
    {
        return result.IsSuccess
            ? Results.Created(uriFactory(result.Value), result.Value)
            : CreateProblemDetails(result.Errors);
    }

    public static IResult ToResult<T>(this Result<T> result, Func<T, IResult> onSuccess)
    {
        return result.IsSuccess
            ? onSuccess(result.Value)
            : CreateProblemDetails(result.Errors);
    }

    public static IResult ToResult(this Result result, Func<IResult> onSuccess)
    {
        return result.IsSuccess
            ? onSuccess()
            : CreateProblemDetails(result.Errors);
    }

    private static IResult CreateProblemDetails(IEnumerable<IError> errors)
    {
        var firstError = errors.FirstOrDefault();
        var (statusCode, title) = GetStatusCodeAndTitle(firstError);

        var errorDetails = errors.Select(e => new
        {
            message = e.Message,
            code = e.Metadata.TryGetValue("code", out var code) ? code?.ToString() : null,
            metadata = e.Metadata.Where(m => m.Key != "code").ToDictionary(m => m.Key, m => m.Value)
        }).ToArray();

        return Results.Problem(
            title: title,
            detail: string.Join("; ", errors.Select(e => e.Message)),
            statusCode: statusCode,
            extensions: new Dictionary<string, object?>
            {
                { "errors", errorDetails }
            }
        );
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
}
