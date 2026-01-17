using ChoreNotifier.Data;
using ChoreNotifier.Infrastructure.Clock;
using ChoreNotifier.Models;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ChoreNotifier.Features.Statistics.GetUserStatistics;

public record GetUserStatisticsRequest(int UserId, DateTimeOffset? StartDate, DateTimeOffset? EndDate)
    : IRequest<Result<GetUserStatisticsResponse>>;

public record GetUserStatisticsResponse(
    int TotalChoresAssigned,
    int TotalChoresCompleted,
    float SnoozeFrequency,
    TimeSpan AverageCompletionTime
);

public sealed class GetUserStatisticsHandler(
    ChoreDbContext db,
    IClock clock
) : IRequestHandler<GetUserStatisticsRequest, Result<GetUserStatisticsResponse>>
{
    public async Task<Result<GetUserStatisticsResponse>> Handle(GetUserStatisticsRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user == null)
            return Result.Fail(new NotFoundError("User", request.UserId));

        var choreOccurencesQuery = db.ChoreOccurrences
            .Where(co => co.User.Id == request.UserId)
            .Where(co => request.StartDate == null || co.DueAt >= request.StartDate)
            .Where(co => request.EndDate == null || co.DueAt <= request.EndDate);

        var totalChoresAssigned = await choreOccurencesQuery
            .Where(co => co.User.Id == request.UserId)
            .CountAsync(cancellationToken);
        var totalChoresCompleted = await choreOccurencesQuery
            .Where(co => co.User.Id == request.UserId && co.CompletedAt != null)
            .CountAsync(cancellationToken);
        var totalSnoozedChores = await choreOccurencesQuery
            .Where(co => co.User.Id == request.UserId && co.ScheduledFor != co.DueAt)
            .CountAsync(cancellationToken);
        float snoozeFrequency = totalChoresAssigned == 0 ? 0 : (float)totalSnoozedChores / totalChoresAssigned;

        var durations = await choreOccurencesQuery
            .Where(co => co.User.Id == request.UserId && co.CompletedAt != null)
            .Select(co => new
            {
                Completes = co.CompletedAt!.Value,
                Due = co.DueAt
            })
            .ToListAsync(cancellationToken);

        var averageTicks = durations
            .Select(x => (x.Completes - x.Due).Ticks)
            .DefaultIfEmpty(0)
            .Average();

        var averageCompletionTime = TimeSpan.FromTicks((long)averageTicks);

        return new GetUserStatisticsResponse(
            TotalChoresAssigned: totalChoresAssigned,
            TotalChoresCompleted: totalChoresCompleted,
            SnoozeFrequency: snoozeFrequency,
            AverageCompletionTime: averageCompletionTime
        );
    }
}
