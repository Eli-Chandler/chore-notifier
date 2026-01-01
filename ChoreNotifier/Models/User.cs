using System.ComponentModel.DataAnnotations;

namespace ChoreNotifier.Models;

public class User
{
    public int Id { get; private set; }
    [MaxLength(100)]
    public required string Name { get; set; }
    private readonly List<ChoreAssignee> _assignments = [];
    public IReadOnlyCollection<ChoreAssignee> Assignments => _assignments;
}
