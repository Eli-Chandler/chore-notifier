using ChoreNotifier.Data;
using ChoreNotifier.Common;
using ChoreNotifier.Models;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace ChoreNotifier.Features.Users.DeleteUser;


public sealed class DeleteUserHandler
{
    private readonly ChoreDbContext _db;
    public DeleteUserHandler(ChoreDbContext db) => _db = db;

    public async Task<Result> Handle(int userId, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            return Result.Fail(new NotFoundError("User", userId.ToString()));

        _db.Users.Remove(user);
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}