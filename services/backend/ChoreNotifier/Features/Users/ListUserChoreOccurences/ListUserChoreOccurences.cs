using ChoreNotifier.Common;
using ChoreNotifier.Data;
using ChoreNotifier.Models;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ChoreNotifier.Features.Users.ListUserChoreOccurences;

public sealed record ListUserChoreOccurrencesRequest(int UserId, int PageSize, int? AfterId)
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

public class ListUserChoreOccurencesHandler(ChoreDbContext db, IClock clock) : IRequestHandler<ListUserChoreOccurrencesRequest, Result<KeysetPage<ListUserChoreOccurrencesResponseItem, int>>>
{
    public async Task<Result<KeysetPage<ListUserChoreOccurrencesResponseItem, int>>> Handle(ListUserChoreOccurrencesRequest request, CancellationToken cancellationToken = default)
    {
        var validatePageSizeResult = ValidatePageSize(request.PageSize);
        if (validatePageSizeResult.IsFailed)
            return validatePageSizeResult;

        var userExists = await db.Users.AnyAsync(u => u.Id == request.UserId, cancellationToken);

        if (!userExists)
            return Result.Fail(new NotFoundError("User", request.UserId));

        var result = await db.ChoreOccurrences
            .Where(co => co.User.Id == request.UserId)

            .OrderBy(co => co.CompletedAt == null)
            .ThenBy(co => co.DueAt)
            .ThenBy(co => co.Id)
            .Include(co => co.Chore)
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

    private static Result ValidatePageSize(int pageSize)
    {
        if (pageSize <= 0)
            return Result.Fail(new ValidationError("Page size must be greater than 0"));
        if (pageSize > 100)
            return Result.Fail(new ValidationError("Page size cannot exceed 100"));
        return Result.Ok();
    }
}
