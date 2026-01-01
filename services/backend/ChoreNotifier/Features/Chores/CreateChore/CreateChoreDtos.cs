using System.ComponentModel.DataAnnotations;
using ChoreNotifier.Models;
using FluentValidation;

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

public sealed class CreateChoreRequestValidator : AbstractValidator<CreateChoreRequest>
{
    public CreateChoreRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(100).WithMessage("Title cannot exceed 100 characters")
            .MinimumLength(1).WithMessage("Title must be at least 1 character");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters");

        RuleFor(x => x.ChoreSchedule)
            .NotNull().WithMessage("Chore schedule is required")
            .SetValidator(new CreateChoreScheduleRequestValidator());

        RuleFor(x => x.SnoozeDuration)
            .GreaterThan(TimeSpan.Zero).WithMessage("Snooze duration must be greater than zero");

        RuleFor(x => x.AssigneeUserIds)
            .NotNull().WithMessage("Assignee user IDs are required")
            .Must(ids => ids.Distinct().Count() == ids.Count()).WithMessage("Assignee user IDs must be unique")
            .Must(ids => ids.Count() <= 50).WithMessage("Cannot have more than 50 assignees");

    }
}

public sealed class CreateChoreScheduleRequestValidator : AbstractValidator<CreateChoreScheduleRequest>
{
    public CreateChoreScheduleRequestValidator()
    {
        RuleFor(x => x.Start)
            .NotEmpty().WithMessage("Start date is required");

        RuleFor(x => x.IntervalDays)
            .GreaterThan(0).WithMessage("Interval days must be greater than zero");

        RuleFor(x => x.Until)
            .GreaterThan(x => x.Start).When(x => x.Until.HasValue)
            .WithMessage("Until date must be after start date");
    }
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

