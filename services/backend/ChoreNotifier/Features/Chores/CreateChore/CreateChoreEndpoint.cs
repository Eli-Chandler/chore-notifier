using ChoreNotifier.Common;
using MediatR;

namespace ChoreNotifier.Features.Chores.CreateChore;

public class CreateChoreEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/chores", async (CreateChoreRequest request, ISender sender) =>
            {
                var result = await sender.Send(request);
                return result.ToCreatedResponse(chore => $"/api/chores/{chore.Id}");
            })
            .WithName("CreateChore")
            .WithTags("Chores");
    }
}
