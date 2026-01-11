using ChoreNotifier.Common;
using MediatR;

namespace ChoreNotifier.Features.Users.CreateUser;

public class CreateUserEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/users", async (CreateUserRequest request, ISender sender) =>
            {
                var result = await sender.Send(request);
                return result.ToCreatedResponse(user => $"/api/users/{user.Id}");
            })
            .WithName("CreateUser")
            .WithTags("Users")
            .WithProblemDetails();
    }
}
