using ChoreNotifier.Common;
using ChoreNotifier.Data;
using ChoreNotifier.Features.Chores.Scheduling;
using ChoreNotifier.Models;

namespace ChoreNotifier.Features.ChoreOccurences.SnoozeChore;

using FluentResults;
using Microsoft.EntityFrameworkCore;

public sealed record SnoozeChoreRequest(int UserId, int ChoreOccurrenceId);

public class CompleteChoreHandler(ChoreDbContext db, IClock clock)
{
    public async Task<Result> Handle(SnoozeChoreRequest req, CancellationToken ct = default)
    {
        var choreOccurence = await db.ChoreOccurrences
            .Include(co => co.User)
            .FirstOrDefaultAsync(co => co.Id == req.ChoreOccurrenceId, ct);

        if (choreOccurence is null)
            return Result.Fail(new NotFoundError("Chore Occurrence", req.ChoreOccurrenceId));

        var snoozeResult = choreOccurence.Snooze(req.UserId, clock.UtcNow);

        if (snoozeResult.IsFailed)
            return snoozeResult;

        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
