using ChoreNotifier.Common;
using MediatR;

namespace ChoreNotifier.Features.Notifications.GetNotificationPreference;

public class GetNotificationPreferenceEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users/{userId:int}/notification-preference", async (int userId, ISender sender) =>
            {
                var result = await sender.Send(new GetNotificationPreferenceRequest(userId));
                return result.ToResponse();
            })
            .WithName("GetNotificationPreference")
            .WithTags("Notifications")
            .WithProblemDetails();
    }
}
