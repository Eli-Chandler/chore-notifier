using ChoreNotifier.Data;
using ChoreNotifier.Models;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ChoreNotifier.Features.Chores.DeleteChore;

public sealed record DeleteChoreRequest(int ChoreId) : IRequest<Result>;

public sealed class DeleteChoreHandler : IRequestHandler<DeleteChoreRequest, Result>
{
    private readonly ChoreDbContext _db;
    public DeleteChoreHandler(ChoreDbContext db) => _db = db;

    public async Task<Result> Handle(DeleteChoreRequest req, CancellationToken ct = default)
    {
        var chore = await _db.Chores.FirstOrDefaultAsync(c => c.Id == req.ChoreId, ct);
        if (chore is null)
            return Result.Fail(new NotFoundError("Chore", req.ChoreId));

        _db.Chores.Remove(chore);
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
