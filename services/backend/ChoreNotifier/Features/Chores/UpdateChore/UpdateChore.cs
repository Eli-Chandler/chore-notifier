using ChoreNotifier.Data;
using ChoreNotifier.Features.Chores.CreateChore;
using ChoreNotifier.Models;
using FluentResults;


namespace ChoreNotifier.Features.Chores.UpdateChore;

public sealed record UpdateChoreRequest(
    string Title,
    string? Description,
    CreateChoreScheduleRequest? ChoreSchedule,
    TimeSpan SnoozeDuration);

public sealed record UpdateChoreResponse(
    int Id,
    string Title,
    string? Description,
    ChoreScheduleResponse ChoreSchedule,
    TimeSpan? SnoozeDuration);

public sealed class UpdateChoreHandler
{
    private readonly ChoreDbContext _db;
    public UpdateChoreHandler(ChoreDbContext db) => _db = db;

    public async Task<Result<UpdateChoreResponse>> Handle(int choreId, UpdateChoreRequest req, CancellationToken ct)
    {
        var chore = await _db.Chores.FindAsync(new object?[] { choreId }, ct);
        if (chore == null)
            return Result.Fail(new NotFoundError("Chore", choreId));

        var updateResult = Result.Merge(
            chore.UpdateTitle(req.Title),
            chore.UpdateDescription(req.Description),
            chore.UpdateSnoozeDuration(req.SnoozeDuration)
        );

        if (updateResult.IsFailed)
            return updateResult;

        if (req.ChoreSchedule != null)
        {
            var createScheduleResult = ChoreSchedule.Create(
                req.ChoreSchedule.Start,
                req.ChoreSchedule.IntervalDays,
                req.ChoreSchedule.Until
            );
            if (createScheduleResult.IsFailed)
                return createScheduleResult.ToResult<UpdateChoreResponse>();
            chore.UpdateChoreSchedule(createScheduleResult.Value);
        }


        await _db.SaveChangesAsync(ct);

        return new UpdateChoreResponse(
            chore.Id,
            chore.Title,
            chore.Description,
            new ChoreScheduleResponse(
                chore.ChoreSchedule.Start,
                chore.ChoreSchedule.IntervalDays,
                chore.ChoreSchedule.Until
            ),
            chore.SnoozeDuration
        );
    }
}