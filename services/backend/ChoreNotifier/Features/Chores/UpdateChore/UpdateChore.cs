using ChoreNotifier.Data;
using ChoreNotifier.Features.Chores.CreateChore;
using ChoreNotifier.Models;
using FluentResults;
using MediatR;


namespace ChoreNotifier.Features.Chores.UpdateChore;

public sealed record UpdateChoreRequest(
    int ChoreId,
    string Title,
    string? Description,
    CreateChoreScheduleRequest? ChoreSchedule,
    TimeSpan? SnoozeDuration) : IRequest<Result<UpdateChoreResponse>>;

public sealed record UpdateChoreResponse(
    int Id,
    string Title,
    string? Description,
    ChoreScheduleResponse ChoreSchedule,
    TimeSpan? SnoozeDuration);

public sealed class UpdateChoreHandler : IRequestHandler<UpdateChoreRequest, Result<UpdateChoreResponse>>
{
    private readonly ChoreDbContext _db;
    public UpdateChoreHandler(ChoreDbContext db) => _db = db;

    public async Task<Result<UpdateChoreResponse>> Handle(UpdateChoreRequest req, CancellationToken ct = default)
    {
        var chore = await _db.Chores.FindAsync(new object?[] { req.ChoreId }, ct);
        if (chore == null)
            return Result.Fail(new NotFoundError("Chore", req.ChoreId));

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
