using ChoreNotifier.Data;
using ChoreNotifier.Models;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace ChoreNotifier.Features.Chores.RemoveChoreAssignee;

public sealed class RemoveChoreAssigneeHandler(ChoreDbContext db)
{
    public async Task<Result> Handle(int choreId, int userId, CancellationToken ct = default)
    {
        var chore = await db.Chores
            .Include(c => c.Assignees)
            .ThenInclude(a => a.User)
            .FirstOrDefaultAsync(c => c.Id == choreId, ct);
        
        if (chore == null)
            return Result.Fail(new NotFoundError("Chore", choreId.ToString()));

        var user = await db.Users.FindAsync(new object?[] { userId }, ct);
        if (user == null)
            return Result.Fail(new NotFoundError("User", userId.ToString()));
        
        var removeResult = chore.RemoveAssignee(user);
        if (removeResult.IsFailed)
            return Result.Fail(removeResult.Errors);

        await db.SaveChangesAsync(ct);

        return Result.Ok();
    }
}