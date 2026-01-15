using ChoreNotifier.Common;
using MediatR;

namespace ChoreNotifier.Features.Users.GetUser;

public class GetUserEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users/{id:int}", async (ISender sender, int id) =>
            {
                var result = await sender.Send(new GetUserRequest(id));
                return result.ToResponse();
            })
            .WithName("GetUser")
            .WithTags("Users")
            .WithProblemDetails();
    }
}
