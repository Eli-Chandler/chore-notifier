using System.ComponentModel.DataAnnotations;
using ChoreNotifier.Models;

namespace ChoreNotifier.Features.Chores.CreateChore;

public sealed record CreateChoreRequest
{
    [MinLength(1), MaxLength(100)] 
    public required string Title { get; init; }
    [MaxLength(1000)]
    public string? Description { get; init; }
    public required CreateChoreScheduleRequest ChoreSchedule { get; init; }
    public bool AllowSnooze { get; init; } = false;
    public TimeSpan SnoozeDuration { get; init; } = TimeSpan.FromDays(1);
    [MaxLength(50)]
    public required IEnumerable<int> AssigneeUserIds { get; init; }
}

public record CreateChoreScheduleRequest
{
    public required DateTimeOffset Start { get; init; }
    public required int IntervalDays { get; init; }
    public DateTimeOffset? Until { get; init; }
}

public class ChoreScheduleResponse
{
    public required DateTimeOffset Start { get; init; }
    public required int IntervalDays { get; init; }
    public DateTimeOffset? Until { get; init; }
}

public class ChoreAssigneeResponse
{
    public required int Id { get; init; }
    public required string Name { get; init; }
}

public class CreateChoreResponse
{
    public required int Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required ChoreScheduleResponse ChoreSchedule { get; init; }
    public required bool AllowSnooze { get; init; }
    public required TimeSpan SnoozeDuration { get; init; }
    public required List<ChoreAssigneeResponse> Assignees { get; init; }
}

