using ChoreNotifier.Common;
using MediatR;

namespace ChoreNotifier.Features.Users.ListUserChoreOccurences;

public class ListUserChoreOccurrencesEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users/{userId:int}/chore-occurrences",
                async (int userId, ISender sender, int pageSize = 20, int? afterId = null, ChoreOccurenceFilter filter = ChoreOccurenceFilter.All) =>
                {
                    var result = await sender.Send(new ListUserChoreOccurrencesRequest(userId, pageSize, afterId, filter));
                    return result.ToResponse();
                })
            .WithName("ListUserChoreOccurrences")
            .WithTags("Users")
            .WithProblemDetails();
    }
}
