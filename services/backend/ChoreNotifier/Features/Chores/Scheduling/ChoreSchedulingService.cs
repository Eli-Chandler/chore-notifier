using ChoreNotifier.Data;
using ChoreNotifier.Models;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace ChoreNotifier.Features.Chores.Scheduling;

public class ChoreSchedulingService
{
    public async Task<Result<ChoreOccurrence>> ScheduleNextOccurrenceIfNeeded(
        ChoreDbContext db,
        Chore chore,
        DateTimeOffset after,
        ChoreOccurrence? lastOccurrence = null
    )
    {
        bool hasPendingOccurrence;
        if (lastOccurrence is null)
        {
            hasPendingOccurrence = await db.ChoreOccurrences
                .AnyAsync(co => co.Chore.Id == chore.Id && co.CompletedAt == null);
        }
        else
        {
            hasPendingOccurrence = lastOccurrence.CompletedAt is null;
        }


        if (hasPendingOccurrence)
        {
            return Result.Fail(new ValidationError("There is already a pending occurrence for this chore."));
        }

        var nextTime = chore.ChoreSchedule.NextAfter(after);
        if (nextTime is null)
            return Result.Fail(new InvalidOperationError("No next occurence is scheduled."));

        var nextAssignee = chore.GetAndIncrementCurrentAssignee();
        if (nextAssignee.IsFailed)
            return Result.Fail(nextAssignee.Errors)
                .WithError(new InvalidOperationError("No next assignee could be determined for the chore."));

        var occurrence = new ChoreOccurrence(chore, nextAssignee.Value, nextTime.Value);
        db.ChoreOccurrences.Add(occurrence);

        return Result.Ok(occurrence);
    }
}
