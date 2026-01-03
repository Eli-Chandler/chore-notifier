using System.Diagnostics.CodeAnalysis;
using FluentResults;

namespace ChoreNotifier.Models;

public class ChoreOccurrence
{
    public int Id { get; private set; }
    public int ChoreId { get; private set; }
    public Chore Chore { get; private set; } = null!; // EF
    public User User { get; private set; } = null!; // EF

    public required DateTimeOffset ScheduledFor { get; set; }
    public DateTimeOffset DueAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    private ChoreOccurrence()
    {
    } // For EF

    [SetsRequiredMembers]
    public ChoreOccurrence(Chore chore, User user, DateTimeOffset scheduledFor)
    {
        Chore = chore;
        User = user;

        ScheduledFor = scheduledFor;
        DueAt = scheduledFor;
    }

    public Result Snooze(int actorUserId, DateTimeOffset currentTime, TimeSpan? duration = null)
    {
        var validationResult = ValidateActor(actorUserId);
        if (validationResult.IsFailed)
            return validationResult;

        if (Chore.SnoozeDuration is null)
            return Result.Fail(new InvalidOperationError("Snoozing is not allowed for this chore."));
        if (currentTime < DueAt)
            return Result.Fail(new InvalidOperationError("Cannot snooze a chore occurrence before its due time."));
        if (CompletedAt is not null)
            return Result.Fail(new InvalidOperationError("Cannot snooze a completed chore occurrence"));

        DueAt = currentTime.Add(duration ?? Chore.SnoozeDuration.Value);
        return Result.Ok();
    }

    public Result Complete(int actorUserId, DateTimeOffset currentTime)
    {
        var validationResult = ValidateActor(actorUserId);
        if (validationResult.IsFailed)
            return validationResult;

        if (CompletedAt is not null)
            return Result.Fail(new InvalidOperationError("Chore occurrence is already completed."));


        CompletedAt = currentTime;
        return Result.Ok();
    }

    private Result ValidateActor(int actorUserId)
    {
        if (actorUserId != User.Id)
            return Result.Fail(new ForbiddenError("User does not have permission to modify this chore occurrence."));
        return Result.Ok();
    }
}
