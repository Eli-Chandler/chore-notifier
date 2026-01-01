using System.Diagnostics.CodeAnalysis;

namespace ChoreNotifier.Models;

public class ChoreOccurrence
{
    public int Id { get; private set; }
    public required Chore Chore { get; set; }
    public required User User { get; set; }
    
    public required DateTimeOffset ScheduledFor { get; set; }
    public DateTimeOffset DueAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    
    private ChoreOccurrence() { } // For EF

    [SetsRequiredMembers]
    public ChoreOccurrence(Chore chore, User user, DateTimeOffset scheduledFor)
    {
        Chore = chore;
        User = user;

        ScheduledFor = scheduledFor;
        DueAt = scheduledFor;
    }
    
    public void Snooze(TimeSpan? duration = null)
    {
        if (!Chore.AllowSnooze)
        {
            throw new InvalidOperationException($@"Chore {Chore.Id} does not allow snoozing.");
        }

        DueAt += duration ?? Chore.SnoozeDuration;
    }
    
    public void Complete(DateTimeOffset? at = null)
    {
        CompletedAt = at ?? DateTimeOffset.UtcNow;
    }
}
