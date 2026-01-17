using ChoreNotifier.Data;
using ChoreNotifier.Features.Chores.Scheduling;
using ChoreNotifier.Infrastructure.Clock;
using ChoreNotifier.Models;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ChoreNotifier.Features.Chores.CreateChore;

public sealed class CreateChoreHandler(ChoreDbContext db, ChoreSchedulingService choreSchedulingService, IClock clock)
    : IRequestHandler<CreateChoreRequest, Result<CreateChoreResponse>>
{
    public async Task<Result<CreateChoreResponse>> Handle(CreateChoreRequest req, CancellationToken ct = default)
    {
        var users = await db.Users.Where(u => req.AssigneeUserIds.Contains(u.Id)).ToListAsync(ct);

        var missingUserIds = req.AssigneeUserIds.Except(users.Select(u => u.Id)).ToList();
        if (missingUserIds.Any())
            return Result.Fail(new NotFoundError("Users", string.Join(", ", missingUserIds)));

        var choreScheduleResult = ChoreSchedule.Create(
            req.ChoreSchedule.Start,
            req.ChoreSchedule.IntervalDays,
            req.ChoreSchedule.Until
        );

        if (choreScheduleResult.IsFailed)
            return Result.Fail(choreScheduleResult.Errors);

        var choreResult = Chore.Create(
            title: req.Title,
            description: req.Description,
            choreSchedule: choreScheduleResult.Value,
            snoozeDuration: req.SnoozeDuration
        );

        if (choreResult.IsFailed)
            return Result.Fail(choreResult.Errors);

        var chore = choreResult.Value;

        db.Chores.Add(chore);

        var usersById = users.ToDictionary(u => u.Id);
        foreach (var userId in req.AssigneeUserIds)
        {
            chore.AddAssignee(usersById[userId]);
        }

        await choreSchedulingService.ScheduleNextOccurrenceIfNeeded(db, chore, clock.UtcNow);

        await db.SaveChangesAsync(ct);

        return chore.ToResponse();
    }
}
