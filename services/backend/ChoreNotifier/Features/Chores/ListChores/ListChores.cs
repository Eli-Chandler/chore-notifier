using ChoreNotifier.Common;
using ChoreNotifier.Data;
using ChoreNotifier.Features.Chores.CreateChore;
using FluentResults;

namespace ChoreNotifier.Features.Chores.ListChores;

public sealed record ListChoresResponseItem(
    int Id,
    string Title,
    string? Description,
    ChoreScheduleResponse? ChoreSchedule,
    TimeSpan? SnoozeDuration
);

public sealed class ListChoresHandler
{
    private readonly ChoreDbContext _db;
    public ListChoresHandler(ChoreDbContext db) => _db = db;

    public async Task<Result<KeysetPage<ListChoresResponseItem, int>>> Handle(int pageSize, int? afterId, CancellationToken ct = default)
    {
        var validatePageSizeResult = ValidatePageSize(pageSize);
        if (validatePageSizeResult.IsFailed)
            return validatePageSizeResult;
        
        var result = await _db.Chores
            .OrderBy(c => c.Id)
            .Where(c => afterId == null || c.Id > afterId)
            .ToKeysetPageAsync(
                pageSize,
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
                c.SnoozeDuration
            )
        );
    }
    
    private static Result ValidatePageSize(int pageSize)
    {
        if (pageSize <= 0)
            return Result.Fail("Page size must be greater than 0");
        if (pageSize > 100)
            return Result.Fail("Page size cannot exceed 100");
        return Result.Ok();
    }
}