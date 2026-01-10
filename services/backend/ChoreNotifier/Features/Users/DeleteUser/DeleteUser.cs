using ChoreNotifier.Data;
using ChoreNotifier.Common;
using ChoreNotifier.Models;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ChoreNotifier.Features.Users.DeleteUser;

public sealed record DeleteUserRequest(int UserId) : IRequest<Result>;

public sealed class DeleteUserHandler : IRequestHandler<DeleteUserRequest, Result>
{
    private readonly ChoreDbContext _db;
    public DeleteUserHandler(ChoreDbContext db) => _db = db;

    public async Task<Result> Handle(DeleteUserRequest req, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == req.UserId, ct);
        if (user is null)
            return Result.Fail(new NotFoundError("User", req.UserId.ToString()));

        _db.Users.Remove(user);
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
