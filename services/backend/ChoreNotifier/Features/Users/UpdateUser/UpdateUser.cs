using System.ComponentModel.DataAnnotations;
using ChoreNotifier.Models;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace ChoreNotifier.Features.Users.UpdateUser;

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

    public async Task<Result<UpdateUserResponse>> Handle(int userId, UpdateUserRequest req, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            return Result.Fail(new NotFoundError("User", userId.ToString()));

        var renameResult = user.Rename(req.Name);
        
        if (renameResult.IsFailed)
            return Result.Fail(renameResult.Errors);

        await _db.SaveChangesAsync(ct);

        return new UpdateUserResponse(user.Id, user.Name);
    }
}