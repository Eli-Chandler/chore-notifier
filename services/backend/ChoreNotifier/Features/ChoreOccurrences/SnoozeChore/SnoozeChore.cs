using ChoreNotifier.Data;
using ChoreNotifier.Infrastructure.Clock;
using ChoreNotifier.Models;

namespace ChoreNotifier.Features.ChoreOccurrences.SnoozeChore;

using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;

public sealed record SnoozeChoreRequest(int UserId, int ChoreOccurrenceId) : IRequest<Result>;

public class SnoozeChoreHandler(ChoreDbContext db, IClock clock)
    : IRequestHandler<SnoozeChoreRequest, Result>
{
    public async Task<Result> Handle(SnoozeChoreRequest req, CancellationToken ct = default)
    {
        var choreOccurence = await db.ChoreOccurrences
            .Include(co => co.User)
            .Include(co => co.Chore)
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
