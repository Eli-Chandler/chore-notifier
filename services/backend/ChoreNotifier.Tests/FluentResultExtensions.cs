using FluentResults;

namespace ChoreNotifier.Tests;

public static class FluentResultExtensions
{
    public static Result ThrowIfFailed(this Result result)
    {
        if (result.IsFailed)
        {
            // You can customize the exception type too
            throw new InvalidOperationException(
                $"Operation failed: {string.Join(", ", result.Errors.Select(e => e.Message))}"
            );
        }

        return result;
    }

    public static Result<T> ThrowIfFailed<T>(this Result<T> result)
    {
        if (result.IsFailed)
        {
            throw new InvalidOperationException(
                $"Operation failed: {string.Join(", ", result.Errors.Select(e => e.Message))}"
            );
        }

        return result;
    }
}
