using ChoreNotifier.Common;
using ChoreNotifier.Data;
using ChoreNotifier.Models;
using FluentResults;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ChoreNotifier.Features.Chores.CreateChore;

public sealed class CreateChoreHandler(ChoreDbContext db)
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

        foreach (var user in users)
        {
            chore.AddAssignee(user);
        }

        await db.SaveChangesAsync(ct);

        return chore.ToResponse();
    }
}

