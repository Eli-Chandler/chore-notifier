using ChoreNotifier.Common;
using MediatR;

namespace ChoreNotifier.Features.Users.ListUsers;

public class ListUsersEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users", async (ISender sender, int pageSize = 20, int? afterId = null) =>
            {
                var result = await sender.Send(new ListUsersRequest(pageSize, afterId));
                return result.ToResult();
            })
            .WithName("ListUsers")
            .WithTags("Users");
    }
}
