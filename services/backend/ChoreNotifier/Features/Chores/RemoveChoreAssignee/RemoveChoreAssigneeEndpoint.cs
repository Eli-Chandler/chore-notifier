using ChoreNotifier.Common;
using MediatR;

namespace ChoreNotifier.Features.Chores.RemoveChoreAssignee;

public class RemoveChoreAssigneeEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/chores/{choreId:int}/assignees/{userId:int}", async (int choreId, int userId, ISender sender) =>
            {
                var result = await sender.Send(new RemoveChoreAssigneeRequest(choreId, userId));
                return result.ToResponse();
            })
            .WithName("RemoveChoreAssignee")
            .WithTags("Chores")
            .WithProblemDetails();
    }
}
