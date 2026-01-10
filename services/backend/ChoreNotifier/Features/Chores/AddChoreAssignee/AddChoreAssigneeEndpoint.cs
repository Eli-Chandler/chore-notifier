using ChoreNotifier.Common;
using MediatR;

namespace ChoreNotifier.Features.Chores.AddChoreAssignee;

public class AddChoreAssigneeEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/chores/{choreId:int}/assignees/{userId:int}", async (int choreId, int userId, ISender sender) =>
            {
                var result = await sender.Send(new AddChoreAssigneeRequest(choreId, userId));
                return result.ToResponse();
            })
            .WithName("AddChoreAssignee")
            .WithTags("Chores");
    }
}
