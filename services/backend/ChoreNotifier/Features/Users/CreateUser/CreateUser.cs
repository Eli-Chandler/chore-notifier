using System.ComponentModel.DataAnnotations;
using ChoreNotifier.Data;
using ChoreNotifier.Models;
using FluentResults;
using FluentValidation;
using MediatR;

namespace ChoreNotifier.Features.Users.CreateUser;

public sealed record CreateUserRequest(string Name) : IRequest<Result<CreateUserResponse>>;

public sealed record CreateUserResponse(int Id, string Name);

public sealed class CreateUserHandler(ChoreDbContext db) : IRequestHandler<CreateUserRequest, Result<CreateUserResponse>>
{

    public async Task<Result<CreateUserResponse>> Handle(CreateUserRequest req, CancellationToken ct = default)
    {

        var createUserResult = User.Create(req.Name);
        if (createUserResult.IsFailed)
            return Result.Fail(createUserResult.Errors);

        var user = createUserResult.Value;

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        return new CreateUserResponse(user.Id, user.Name);
    }
}
