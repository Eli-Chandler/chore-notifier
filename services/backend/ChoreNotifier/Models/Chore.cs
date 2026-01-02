using System.ComponentModel.DataAnnotations;
using ChoreNotifier.Common;
using FluentResults;

namespace ChoreNotifier.Models;

public class Chore
{
    public int Id { get; private set; }
    [MaxLength(100)] public string Title { get; private set; }
    [MaxLength(1000)] public string? Description { get; private set; }
    public ChoreSchedule ChoreSchedule { get; private set; }
    public TimeSpan? SnoozeDuration { get; private set; } = TimeSpan.FromDays(1);

    private readonly List<ChoreAssignee> _assignees = [];
    public IReadOnlyCollection<ChoreAssignee> Assignees => _assignees;

    public int NextAssigneeIndex { get; private set; } = 0;

    private Chore()
    {
    } // For EF

    private Chore(string title, ChoreSchedule choreSchedule, string? description = null,
        TimeSpan? snoozeDuration = null)
    {
        Title = title;
        Description = description;
        ChoreSchedule = choreSchedule;
        SnoozeDuration = snoozeDuration;
    }

    public static Result<Chore> Create(string title, ChoreSchedule choreSchedule, string? description = null,
        TimeSpan? snoozeDuration = null)
    {
        var validationResult = Result.Merge(
            ValidateTitle(title),
            ValidateDescription(description),
            ValidateSnoozeDuration(snoozeDuration ?? TimeSpan.FromDays(1))
        );

        if (validationResult.IsFailed)
            return validationResult;

        var chore = new Chore(title, choreSchedule, description, snoozeDuration);
        return chore;
    }

    private static Result ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Result.Fail(new ValidationError("Title cannot be empty."));
        if (title.Length > 100)
            return Result.Fail(new ValidationError("Title cannot exceed 100 characters."));
        return Result.Ok();
    }

    private static Result ValidateDescription(string? description)
    {
        if (description != null && description.Length > 1000)
            return Result.Fail(new ValidationError("Description cannot exceed 1000 characters."));
        return Result.Ok();
    }

    private static Result ValidateSnoozeDuration(TimeSpan snoozeDuration)
    {
        if (snoozeDuration <= TimeSpan.Zero)
            return Result.Fail(new ValidationError("SnoozeDuration must be greater than zero."));
        return Result.Ok();
    }


    private static readonly IEqualityComparer<User> UserComparer =
        new EfEntityIdentityComparer<User>(u => u.Id);

    public Result<User> GetAndIncrementCurrentAssignee()
    {
        var ordered = OrderedAssignees();
        if (ordered.Count == 0)
            return Result.Fail(new InvalidOperationError("No assignees are assigned to this chore."));

        NextAssigneeIndex = NormalizeIndex(NextAssigneeIndex, ordered.Count);

        var user = ordered[NextAssigneeIndex].User;
        NextAssigneeIndex = (NextAssigneeIndex + 1) % ordered.Count;
        return user;
    }

    public Result<ChoreAssignee> AddAssignee(User user, int? order = null)
    {
        if (_assignees.Any(a => UserComparer.Equals(a.User, user)))
            return Result.Fail(new ConflictError("User is already an assignee."));

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

        var assignee = new ChoreAssignee { User = user, Chore = this, Order = insertIndex };
        _assignees.Add(assignee);

        ordered.Insert(insertIndex, assignee);
        ReIndexAssignees(ordered);

        NextAssigneeIndex = NormalizeIndex(NextAssigneeIndex, ordered.Count);
        return Result.Ok(assignee);
    }

    public Result RemoveAssignee(User user)
    {
        var ordered = OrderedAssignees();

        var removeIndex = ordered.FindIndex(a => UserComparer.Equals(a.User, user));
        if (removeIndex < 0)
            return Result.Fail(new InvalidOperationError("User is not assigned to this chore."));

        NextAssigneeIndex = NormalizeIndex(NextAssigneeIndex, ordered.Count);

        var toRemove = ordered[removeIndex];
        _assignees.Remove(toRemove);

        ordered.RemoveAt(removeIndex);
        ReIndexAssignees(ordered);

        if (ordered.Count == 0)
        {
            NextAssigneeIndex = 0;
            return Result.Ok();
        }

        if (removeIndex < NextAssigneeIndex)
            NextAssigneeIndex--;

        NextAssigneeIndex = NormalizeIndex(NextAssigneeIndex, ordered.Count);
        return Result.Ok();
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
    
    public Result UpdateTitle(string title)
    {
        var validationResult = ValidateTitle(title);
        if (validationResult.IsFailed)
            return validationResult;

        Title = title;
        return Result.Ok();
    }
    
    public Result UpdateDescription(string? description)
    {
        var validationResult = ValidateDescription(description);
        if (validationResult.IsFailed)
            return validationResult;

        Description = description;
        return Result.Ok();
    }
    
    public Result UpdateSnoozeDuration(TimeSpan? snoozeDuration)
    {
        var validationResult = ValidateSnoozeDuration(snoozeDuration ?? TimeSpan.FromDays(1));
        if (validationResult.IsFailed)
            return validationResult;

        SnoozeDuration = snoozeDuration;
        return Result.Ok();
    }
    
    public void UpdateChoreSchedule(ChoreSchedule choreSchedule)
    {
        ChoreSchedule = choreSchedule;
    }
}

public class ChoreAssignee
{
    public int Id { get; private set; }
    public required User User { get; init; }
    public required Chore Chore { get; init; }
    public required int Order { get; set; }
}