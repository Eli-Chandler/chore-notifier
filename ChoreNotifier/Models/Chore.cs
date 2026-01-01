using System.ComponentModel.DataAnnotations;
using ChoreNotifier.Common;

namespace ChoreNotifier.Models;

public class Chore
{
    public int Id { get; private set; }
    [MaxLength(100)]
    public required string Title { get; set; }
    [MaxLength(1000)]
    public string? Description { get; set; }
    public required ChoreSchedule ChoreSchedule { get; set; }
    public bool AllowSnooze { get; set; } = false;
    public TimeSpan SnoozeDuration { get; set; } = TimeSpan.FromDays(1);

    private readonly List<ChoreAssignee> _assignees = [];
    public IReadOnlyCollection<ChoreAssignee> Assignees => _assignees;

    public int NextAssigneeIndex { get; private set; }

    private static readonly IEqualityComparer<User> UserComparer =
        new EfEntityIdentityComparer<User>(u => u.Id);

    public User GetAndIncrementCurrentAssignee()
    {
        var ordered = OrderedAssignees();
        if (ordered.Count == 0)
            throw new InvalidOperationException($"Chore {Id} has no assignees.");

        NextAssigneeIndex = NormalizeIndex(NextAssigneeIndex, ordered.Count);

        var user = ordered[NextAssigneeIndex].User;
        NextAssigneeIndex = (NextAssigneeIndex + 1) % ordered.Count;
        return user;
    }

    public void AddAssignee(User user, int? order = null)
    {
        if (_assignees.Any(a => UserComparer.Equals(a.User, user)))
            throw new InvalidOperationException($"User {user.Id} is already an assignee.");

        var ordered = OrderedAssignees();

        var insertIndex = order ?? ordered.Count;
        if (insertIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(order), "Order must be >= 0.");
        if (insertIndex > ordered.Count)
            throw new ArgumentOutOfRangeException(nameof(order), $"Order must be <= {ordered.Count}.");

        // Keep the same "next" person after insertion.
        if (ordered.Count > 0)
        {
            NextAssigneeIndex = NormalizeIndex(NextAssigneeIndex, ordered.Count);
            if (insertIndex <= NextAssigneeIndex)
                NextAssigneeIndex++;
        }

        var assignee = new ChoreAssignee { User = user, Chore = this, Order = insertIndex};
        _assignees.Add(assignee);

        ordered.Insert(insertIndex, assignee);
        ReIndexAssignees(ordered);

        NextAssigneeIndex = NormalizeIndex(NextAssigneeIndex, ordered.Count);
    }

    public void RemoveAssignee(User user)
    {
        var ordered = OrderedAssignees();

        var removeIndex = ordered.FindIndex(a => UserComparer.Equals(a.User, user));
        if (removeIndex < 0)
            throw new InvalidOperationException($"User {user.Id} is not an assignee.");

        NextAssigneeIndex = NormalizeIndex(NextAssigneeIndex, ordered.Count);

        var toRemove = ordered[removeIndex];
        _assignees.Remove(toRemove);

        ordered.RemoveAt(removeIndex);
        ReIndexAssignees(ordered);

        if (ordered.Count == 0)
        {
            NextAssigneeIndex = 0;
            return;
        }
        
        if (removeIndex < NextAssigneeIndex)
            NextAssigneeIndex--;

        NextAssigneeIndex = NormalizeIndex(NextAssigneeIndex, ordered.Count);
    }

    private List<ChoreAssignee> OrderedAssignees() =>
        _assignees
            .OrderBy(a => a.Order)
            .ThenBy(a => a.Id) // tie-break if DB order is inconsistent
            .ToList();

    private static int NormalizeIndex(int index, int count)
    {
        if (count <= 0) return 0;
        // guard against stale values (e.g., data fixups, manual edits, etc.)
        var m = index % count;
        return m < 0 ? m + count : m;
    }

    private static void ReIndexAssignees(List<ChoreAssignee> assignees)
    {
        for (var i = 0; i < assignees.Count; i++)
            assignees[i].Order = i;
    }
}


public class ChoreAssignee
{
    public int Id { get; private set; }
    public required User User { get; init; }
    public required Chore Chore { get; init; }
    public required int Order { get; set; }
}

public abstract class ChoreSchedule
{
    public required DateTimeOffset Start { get; init; }
    public DateTimeOffset? Until { get; init; }
    
    public abstract DateTimeOffset? NextAfter(DateTimeOffset after);
}

public sealed class WeekdayAndTimeChoreSchedule : ChoreSchedule
{
    public required DayOfWeek Weekday { get; init; }
    public required TimeOnly Time { get; init; }

    public override DateTimeOffset? NextAfter(DateTimeOffset after)
    {
        var offset = Start.Offset;

        var afterLocal = after.ToOffset(offset).DateTime;
        var startLocal = Start.DateTime;

        if (afterLocal < startLocal)
            afterLocal = startLocal;

        var today = afterLocal.Date;

        int daysUntil =
            ((int)Weekday - (int)today.DayOfWeek + 7) % 7;

        var candidateLocal =
            today.AddDays(daysUntil) + Time.ToTimeSpan();

        if (candidateLocal <= afterLocal)
            candidateLocal = candidateLocal.AddDays(7);

        var next = new DateTimeOffset(candidateLocal, offset);

        if (Until is not null && next > Until.Value)
            return null;

        return next;
    }
}
