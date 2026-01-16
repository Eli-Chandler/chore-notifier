using ChoreNotifier.Common;
using MediatR;

namespace ChoreNotifier.Features.Notifications.AddNotificationPreference;

public class AddNotificationPreferenceEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/users/{userId:int}/notification-preferences", async (int userId, CreateNotificationMethodRequest methodRequest, ISender sender) =>
            {
                var request = new AddNotificationPreferenceRequest(userId, methodRequest);
                var result = await sender.Send(request);
                return result.ToResponse();
            })
            .WithName("AddNotificationPreference")
            .WithTags("Notifications")
            .WithProblemDetails();
    }
}
