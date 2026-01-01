using System.ComponentModel.DataAnnotations;
using ChoreNotifier.Data;
using ChoreNotifier.Models;

namespace ChoreNotifier.Features.Users;

public sealed record CreateUserRequest
{
    [MinLength(1), MaxLength(100)]
    public required string Name { get; init; }
}

public sealed record CreateUserResponse(int Id, string Name);

public sealed class CreateUserHandler
{
    private readonly ChoreDbContext _db;
    public CreateUserHandler(ChoreDbContext db) => _db = db;

    public async Task<CreateUserResponse> Handle(CreateUserRequest req, CancellationToken ct)
    {
        var user = new User { Name = req.Name };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return new CreateUserResponse(user.Id, user.Name);
    }
}
