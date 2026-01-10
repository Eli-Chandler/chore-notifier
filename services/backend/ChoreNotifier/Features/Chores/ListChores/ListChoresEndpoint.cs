using ChoreNotifier.Common;
using MediatR;

namespace ChoreNotifier.Features.Chores.ListChores;

public class ListChoresEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/chores", async (ISender sender, int pageSize = 20, int? afterId = null) =>
            {
                var result = await sender.Send(new ListChoresRequest(pageSize, afterId));
                return result.ToResult();
            })
            .WithName("ListChores")
            .WithTags("Chores");
    }
}
