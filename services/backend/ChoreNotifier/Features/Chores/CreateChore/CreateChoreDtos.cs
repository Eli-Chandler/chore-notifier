using System.ComponentModel.DataAnnotations;
using ChoreNotifier.Models;

namespace ChoreNotifier.Features.Chores.CreateChore;

public sealed record CreateChoreRequest(
    string Title,
    string? Description,
    CreateChoreScheduleRequest ChoreSchedule,
    TimeSpan SnoozeDuration,
    IEnumerable<int> AssigneeUserIds);

public sealed record CreateChoreScheduleRequest(DateTimeOffset Start, int IntervalDays, DateTimeOffset? Until);

public sealed record ChoreScheduleResponse(DateTimeOffset Start, int IntervalDays, DateTimeOffset? Until);

public sealed record ChoreAssigneeResponse(int Id, String Name);

public sealed record CreateChoreResponse(
    int Id,
    string Title,
    string? Description,
    ChoreScheduleResponse ChoreSchedule,
    TimeSpan? SnoozeDuration,
    IEnumerable<ChoreAssigneeResponse> Assignees);

public static class ChoreMappings
{
    public static CreateChoreResponse ToResponse(this Chore chore)
    {
        return new CreateChoreResponse(
            chore.Id,
            chore.Title,
            chore.Description,
            new ChoreScheduleResponse(
                chore.ChoreSchedule.Start,
                chore.ChoreSchedule.IntervalDays,
                chore.ChoreSchedule.Until),
            chore.SnoozeDuration,
            chore.Assignees
                .OrderBy(a => a.Order)
                .Select(a => new ChoreAssigneeResponse(
                    a.User.Id,
                    a.User.Name))
        );
    }
}
