using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace ChoreNotifier.Models;

[Owned]
public sealed class ChoreSchedule
{
    public DateTimeOffset Start { get; private set; }
    public DateTimeOffset? Until { get; private set; }
    public int IntervalDays { get; private set; }

    private ChoreSchedule()
    {
    } // For EF

    private ChoreSchedule(DateTimeOffset start, int intervalDays, DateTimeOffset? until = null)
    {
        Start = start;
        Until = until;
        IntervalDays = intervalDays;
    }

    public static Result<ChoreSchedule> Create(DateTimeOffset start, int intervalDays, DateTimeOffset? until = null)
    {
        var validationResult = Result.Merge(
            ValidateIntervalDays(intervalDays),
            ValidateUntil(start, until)
        );

        if (validationResult.IsFailed)
            return validationResult;

        var schedule = new ChoreSchedule(start, intervalDays, until);
        return Result.Ok(schedule);
    }

    private static Result ValidateIntervalDays(int intervalDays)
    {
        if (intervalDays <= 0)
            return Result.Fail(new ValidationError("IntervalDays must be > 0."));
        return Result.Ok();
    }

    private static Result ValidateUntil(DateTimeOffset start, DateTimeOffset? until)
    {
        if (until is not null && until < start)
            return Result.Fail(new ValidationError("Until must be >= Start."));
        return Result.Ok();
    }

    public DateTimeOffset? NextAfter(DateTimeOffset after)
    {
        if (IntervalDays <= 0)
            throw new ArgumentOutOfRangeException(nameof(IntervalDays), "IntervalDays must be greater than 0.");

        if (after < Start)
            return Start;

        var elapsedDays = (after - Start).TotalDays;
        var intervalsElapsed = (long)Math.Floor(elapsedDays / IntervalDays);

        var next = Start.AddDays((intervalsElapsed + 1) * IntervalDays);

        if (Until is not null && next > Until.Value)
            return null;

        return next;
    }
}