using ChoreNotifier.Common;
using ChoreNotifier.Data;
using ChoreNotifier.Features.Chores.CreateChore;
using ChoreNotifier.Models;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ChoreNotifier.Features.Chores.ListChores;

public sealed record ListChoresRequest(int PageSize, int? AfterId)
    : IRequest<Result<KeysetPage<ListChoresResponseItem, int>>>;

public sealed record ListChoresResponseItemAssignedUser(
    int Id,
    string Name
);

public sealed record ListChoresResponseItem(
    int Id,
    string Title,
    string? Description,
    ChoreScheduleResponse ChoreSchedule,
    TimeSpan? SnoozeDuration,
    IEnumerable<ListChoresResponseItemAssignedUser> AssignedUsers
);

public sealed class
    ListChoresHandler : IRequestHandler<ListChoresRequest, Result<KeysetPage<ListChoresResponseItem, int>>>
{
    private readonly ChoreDbContext _db;
    public ListChoresHandler(ChoreDbContext db) => _db = db;

    public async Task<Result<KeysetPage<ListChoresResponseItem, int>>> Handle(ListChoresRequest req,
        CancellationToken ct = default)
    {
        var validatePageSizeResult = ValidatePageSize(req.PageSize);
        if (validatePageSizeResult.IsFailed)
            return validatePageSizeResult;

        var result = await _db.Chores
            .OrderBy(c => c.Id)
            .Where(c => req.AfterId == null || c.Id > req.AfterId)
            .Include(c => c.Assignees)
            .ThenInclude(a => a.User)
            .ToKeysetPageAsync(
                req.PageSize,
                c => c.Id,
                ct
            );

        return result.Select(c => new ListChoresResponseItem(
                c.Id,
                c.Title,
                c.Description,
                new ChoreScheduleResponse(
                    c.ChoreSchedule.Start,
                    c.ChoreSchedule.IntervalDays,
                    c.ChoreSchedule.Until
                ),
                c.SnoozeDuration,
                c.Assignees.Select(a => new ListChoresResponseItemAssignedUser(
                        a.User.Id,
                        a.User.Name
                    )
                )
            )
        );
    }

    private static Result ValidatePageSize(int pageSize)
    {
        if (pageSize <= 0)
            return Result.Fail(new ValidationError("Page size must be greater than 0"));
        if (pageSize > 100)
            return Result.Fail(new ValidationError("Page size cannot exceed 100"));
        return Result.Ok();
    }
}
