using System.ComponentModel.DataAnnotations;
using FluentResults;

namespace ChoreNotifier.Models;

public class User
{
    public int Id { get; private set; }
    [MaxLength(100)]
    public string Name { get; private set; }
    private readonly List<ChoreAssignee> _assignments = [];
    public IReadOnlyCollection<ChoreAssignee> Assignments => _assignments;

    private User() { } // For EF

    private User(string name)
    {
        Name = name;
    }

    public static Result<User> Create(string name)
    {
        var validationResult = ValidateName(name);
        if (validationResult.IsFailed)
            return validationResult;
        return Result.Ok(new User(name));
    }

    private static Result ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Fail("Name is required.");
        if (name.Length > 100)
            return Result.Fail("Name cannot exceed 100 characters.");
        return Result.Ok();
    }

    public Result Rename(string newName)
    {
        var validationResult = ValidateName(newName);
        if (validationResult.IsFailed)
            return validationResult;
        Name = newName;
        return Result.Ok();
    }
}
