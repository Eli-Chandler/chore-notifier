using System.ComponentModel.DataAnnotations;
using ChoreNotifier.Data;
using ChoreNotifier.Models;
using FluentValidation;

namespace ChoreNotifier.Features.Users;

public sealed record CreateUserRequest(string Name);

public sealed record CreateUserResponse(int Id, string Name);

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters")
            .MinimumLength(1).WithMessage("Name must be at least 1 character");
    }
}

public sealed class CreateUserHandler(ChoreDbContext db, IValidator<CreateUserRequest> validator)
{

    public async Task<CreateUserResponse> Handle(CreateUserRequest req, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(req, ct);
        var user = new User { Name = req.Name };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        return new CreateUserResponse(user.Id, user.Name);
    }
}
