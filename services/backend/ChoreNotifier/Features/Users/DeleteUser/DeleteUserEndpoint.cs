using ChoreNotifier.Common;
using MediatR;

namespace ChoreNotifier.Features.Users.DeleteUser;

public class DeleteUserEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/users/{userId:int}", async (int userId, ISender sender) =>
            {
                var result = await sender.Send(new DeleteUserRequest(userId));
                return result.ToResponse();
            })
            .WithName("DeleteUser")
            .WithTags("Users");
    }
}
