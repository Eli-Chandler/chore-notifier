using ChoreNotifier.Data;
using ChoreNotifier.Models;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ChoreNotifier.Features.Chores.RemoveChoreAssignee;

public sealed record RemoveChoreAssigneeRequest(int ChoreId, int UserId) : IRequest<Result>;

public sealed class RemoveChoreAssigneeHandler(ChoreDbContext db)
    : IRequestHandler<RemoveChoreAssigneeRequest, Result>
{
    public async Task<Result> Handle(RemoveChoreAssigneeRequest req, CancellationToken ct = default)
    {
        var chore = await db.Chores
            .Include(c => c.Assignees)
            .ThenInclude(a => a.User)
            .FirstOrDefaultAsync(c => c.Id == req.ChoreId, ct);

        if (chore == null)
            return Result.Fail(new NotFoundError("Chore", req.ChoreId.ToString()));

        var user = await db.Users.FindAsync(new object?[] { req.UserId }, ct);
        if (user == null)
            return Result.Fail(new NotFoundError("User", req.UserId.ToString()));

        var removeResult = chore.RemoveAssignee(user);
        if (removeResult.IsFailed)
            return Result.Fail(removeResult.Errors);

        await db.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
