using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace ChoreNotifier.Features.Users;

public sealed record UpdateUserRequest
{
    [MinLength(1), MaxLength(100)]
    public required string Name { get; init; }
}

public sealed record UpdateUserResponse(int Id, string Name);

public sealed class UpdateUserHandler
{
    private readonly Data.ChoreDbContext _db;
    public UpdateUserHandler(Data.ChoreDbContext db) => _db = db;

    public async Task<UpdateUserResponse> Handle(int userId, UpdateUserRequest req, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            throw new Common.NotFoundException("User", userId);

        user.Name = req.Name;

        await _db.SaveChangesAsync(ct);

        return new UpdateUserResponse(user.Id, user.Name);
    }
}