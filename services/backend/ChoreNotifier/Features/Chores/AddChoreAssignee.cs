namespace ChoreNotifier.Features.Chores;

public sealed record AddChoreAssigneeRequest
{
    public required int UserId { get; init; }

}

public sealed class AddChoreAssigneeHandler
{

}