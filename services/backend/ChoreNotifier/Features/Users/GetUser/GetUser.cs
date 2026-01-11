using ChoreNotifier.Data;
using ChoreNotifier.Models;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ChoreNotifier.Features.Users.GetUser;

public sealed record GetUserRequest(int UserId) : IRequest<Result<GetUserResponse>>;

public sealed record GetUserResponse(int Id, string Name);

public sealed class GetUserHandler : IRequestHandler<GetUserRequest, Result<GetUserResponse>>
{
    private readonly ChoreDbContext _db;
    public GetUserHandler(ChoreDbContext db) => _db = db;

    public async Task<Result<GetUserResponse>> Handle(GetUserRequest req, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == req.UserId, ct);
        if (user is null)
            return Result.Fail(new NotFoundError("User", req.UserId.ToString()));

        return new GetUserResponse(user.Id, user.Name);
    }
}
