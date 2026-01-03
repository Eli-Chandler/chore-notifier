using ChoreNotifier.Common;
using ChoreNotifier.Data;
using ChoreNotifier.Features.Chores.Scheduling;
using ChoreNotifier.Models;

namespace ChoreNotifier.Features.ChoreOccurences.CompleteChore;

using FluentResults;
using Microsoft.EntityFrameworkCore;

public sealed record CompleteChoreRequest(int UserId, int ChoreOccurrenceId);

public class CompleteChoreHandler(ChoreDbContext db, ChoreSchedulingService choreSchedulingService, IClock clock)
{
    public async Task<Result> Handle(CompleteChoreRequest req, CancellationToken ct = default)
    {
        var choreOccurence = await db.ChoreOccurrences
            .Include(co => co.User)
            .Include(co => co.Chore)
            .FirstOrDefaultAsync(co => co.Id == req.ChoreOccurrenceId, ct);

        if (choreOccurence is null)
            return Result.Fail(new NotFoundError("Chore Occurrence", req.ChoreOccurrenceId));

        var completeAt = clock.UtcNow;
        var completeResult = choreOccurence.Complete(req.UserId, completeAt);

        if (completeResult.IsFailed)
            return completeResult;

        await choreSchedulingService.ScheduleNextOccurrenceIfNeeded(db, choreOccurence.Chore, completeAt);
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
