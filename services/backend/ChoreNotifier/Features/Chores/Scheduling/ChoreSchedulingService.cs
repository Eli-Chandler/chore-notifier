using ChoreNotifier.Data;
using ChoreNotifier.Models;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace ChoreNotifier.Features.Chores.Scheduling;

public class ChoreSchedulingService
{
    public async Task<Result<ChoreOccurrence>> ScheduleNextOccurrence(
        ChoreDbContext db,
        Chore chore,
        DateTimeOffset after
    )
    {
        var hasPendingOccurrence = await db.ChoreOccurrences
            .AnyAsync(co => co.Chore.Id == chore.Id && co.CompletedAt == null);

        if (hasPendingOccurrence)
        {
            return Result.Fail(new ValidationError("There is already an active occurrence for this chore."));
        }

        var nextTime = chore.ChoreSchedule.NextAfter(after);
        if (nextTime is null)
            return Result.Fail(new ValidationError("No next occurrence time could be determined."));

        var nextAssignee = chore.GetAndIncrementCurrentAssignee();
        if (nextAssignee.IsFailed)
            return Result.Fail(nextAssignee.Errors)
                .WithError("No next assignee could be determined.");

        var occurrence = new ChoreOccurrence(chore, nextAssignee.Value, nextTime.Value);
        db.ChoreOccurrences.Add(occurrence);

        return Result.Ok(occurrence);
    }
}