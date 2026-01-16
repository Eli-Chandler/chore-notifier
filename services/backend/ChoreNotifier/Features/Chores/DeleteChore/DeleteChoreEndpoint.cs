using ChoreNotifier.Common;
using MediatR;

namespace ChoreNotifier.Features.Chores.DeleteChore;

public class DeleteChoreEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/chores/{choreId:int}", async (int choreId, ISender sender) =>
            {
                var result = await sender.Send(new DeleteChoreRequest(choreId));
                return result.ToResponse();
            })
            .WithName("DeleteChore")
            .WithTags("Chores")
            .WithProblemDetails();
    }
}
