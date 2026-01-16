using ChoreNotifier.Common;
using MediatR;

namespace ChoreNotifier.Features.Notifications.ListNotificationHistory;

public class ListNotificationHistoryEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users/{userId:int}/notification-history",
                async (int userId, ISender sender, int pageSize = 20, DateTimeOffset? afterDate = null) =>
                {
                    var result = await sender.Send(new ListNotificationHistoryRequest(userId, pageSize, afterDate));
                    return result.ToResponse();
                })
            .WithName("ListNotificationHistory")
            .WithTags("Notifications")
            .WithProblemDetails();
    }
}
