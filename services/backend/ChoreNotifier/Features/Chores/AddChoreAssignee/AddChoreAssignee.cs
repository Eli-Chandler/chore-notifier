using ChoreNotifier.Common;
using ChoreNotifier.Data;
using ChoreNotifier.Features.Chores.CreateChore;
using ChoreNotifier.Features.Chores.Scheduling;
using ChoreNotifier.Models;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace ChoreNotifier.Features.Chores.AddChoreAssignee;

public sealed record AddChoreAssigneeRequest(int UserId);

public sealed record AddChoreAssigneeResponse(
    int Id,
    string Title,
    string? Description,
    ChoreScheduleResponse ChoreSchedule,
    IEnumerable<ChoreAssigneeResponse> Assignees);

public sealed class AddChoreAssigneeHandler(ChoreDbContext db, ChoreSchedulingService choreSchedulingService, IClock clock)
{
    public async Task<Result<AddChoreAssigneeResponse>> Handle(int choreId, AddChoreAssigneeRequest req,
        CancellationToken ct = default)
    {
        var chore = await db.Chores
            .Include(c => c.Assignees)
            .ThenInclude(a => a.User)
            .FirstOrDefaultAsync(c => c.Id == choreId, ct);

        if (chore == null)
            return Result.Fail(new NotFoundError("Chore", choreId.ToString()));

        var user = await db.Users.FindAsync(new object?[] { req.UserId }, ct);
        if (user == null)
            return Result.Fail(new NotFoundError("User", req.UserId.ToString()));

        var addResult = chore.AddAssignee(user);
        if (addResult.IsFailed)
            return Result.Fail(addResult.Errors);

        if (chore.Assignees.Count == 1) // If there wasn't any assignees before this may not have been scheduled yet
            await choreSchedulingService.ScheduleNextOccurrence(db, chore, clock.UtcNow);
        
        await db.SaveChangesAsync(ct);

        return new AddChoreAssigneeResponse(
            chore.Id,
            chore.Title,
            chore.Description,
            new ChoreScheduleResponse(
                chore.ChoreSchedule.Start,
                chore.ChoreSchedule.IntervalDays,
                chore.ChoreSchedule.Until),
            chore.Assignees.Select(a => new ChoreAssigneeResponse(a.User.Id, a.User.Name))
        );
    }
}