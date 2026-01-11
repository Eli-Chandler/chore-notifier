using ChoreNotifier.Common;
using ChoreNotifier.Data;
using ChoreNotifier.Models;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ChoreNotifier.Features.Users.ListUserChoreOccurences;

public enum ChoreOccurenceFilter
{
    All,
    Completed,
    Upcoming,
    Due
}

public sealed record ListUserChoreOccurrencesRequest(
    int UserId,
    int PageSize,
    int? AfterId = null,
    ChoreOccurenceFilter Filter = ChoreOccurenceFilter.All)
    : IRequest<Result<KeysetPage<ListUserChoreOccurrencesResponseItem, int>>>;

public sealed record ListUserChoreOccurrencesResponseItem(
    int Id,
    DateTimeOffset OriginalDueAt,
    DateTimeOffset CurrentDueAt,
    bool IsCompleted,
    bool IsDue,
    ListUserChoreOccurrencesChoreResponseItem Chore
);

public sealed record ListUserChoreOccurrencesChoreResponseItem(
    int Id,
    string Title,
    string? Description,
    TimeSpan? SnoozeDuration
);

public class ListUserChoreOccurrencesHandler(ChoreDbContext db, IClock clock)
    : IRequestHandler<ListUserChoreOccurrencesRequest, Result<KeysetPage<ListUserChoreOccurrencesResponseItem, int>>>
{
    public async Task<Result<KeysetPage<ListUserChoreOccurrencesResponseItem, int>>> Handle(
        ListUserChoreOccurrencesRequest request, CancellationToken cancellationToken = default)
    {
        var validateResult = Result.Merge(
            ValidatePageSize(request.PageSize),
            ValidateFilter(request.Filter)
        );

        if (validateResult.IsFailed)
            return validateResult.ToResult<KeysetPage<ListUserChoreOccurrencesResponseItem, int>>();

        var userExists = await db.Users.AnyAsync(u => u.Id == request.UserId, cancellationToken);

        if (!userExists)
            return Result.Fail(new NotFoundError("User", request.UserId));

        var query = db.ChoreOccurrences
            .Where(co => co.User.Id == request.UserId)
            .Include(co => co.Chore)
            .AsQueryable();

        query = ApplyFilter(query, request.Filter);

        var result = await query
            .OrderBy(co => co.Id)
            .Where(co => request.AfterId == null || co.Id > request.AfterId)
            .ToKeysetPageAsync(request.PageSize, co => co.Id, cancellationToken);

        return result.Select(co => new ListUserChoreOccurrencesResponseItem(
            co.Id,
            co.ScheduledFor,
            co.DueAt,
            co.IsCompleted,
            co.IsDue(clock.UtcNow),
            new ListUserChoreOccurrencesChoreResponseItem(
                co.Chore.Id,
                co.Chore.Title,
                co.Chore.Description,
                co.Chore.SnoozeDuration
            )
        ));
    }

    private IQueryable<ChoreOccurrence> ApplyFilter(
        IQueryable<ChoreOccurrence> query,
        ChoreOccurenceFilter filter)
    {
        return filter switch
        {
            ChoreOccurenceFilter.All => query,
            ChoreOccurenceFilter.Completed => query.Where(co => co.CompletedAt != null),
            ChoreOccurenceFilter.Upcoming => query.Where(co => co.CompletedAt == null && co.DueAt > clock.UtcNow),
            ChoreOccurenceFilter.Due => query.Where(co => co.CompletedAt == null && co.DueAt <= clock.UtcNow),
            _ => throw new ArgumentOutOfRangeException(nameof(filter), "Invalid filter value")
        };
    }

    private static Result ValidatePageSize(int pageSize)
    {
        if (pageSize <= 0)
            return Result.Fail(new ValidationError("Page size must be greater than 0"));
        if (pageSize > 100)
            return Result.Fail(new ValidationError("Page size cannot exceed 100"));
        return Result.Ok();
    }

    public static Result ValidateFilter(ChoreOccurenceFilter filter)
    {
        if (!Enum.IsDefined(typeof(ChoreOccurenceFilter), filter))
            return Result.Fail(new ValidationError("Invalid filter value"));
        return Result.Ok();
    }
}
