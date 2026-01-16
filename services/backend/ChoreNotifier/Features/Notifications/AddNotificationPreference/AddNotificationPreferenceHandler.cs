using System.Text.Json.Serialization;
using ChoreNotifier.Data;
using ChoreNotifier.Infrastructure.Notifications;
using ChoreNotifier.Models;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ChoreNotifier.Features.Notifications.AddNotificationPreference;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(CreateConsoleMethodRequest), "Console")]
[JsonDerivedType(typeof(CreateNtfyMethodRequest), "Ntfy")]
public abstract record CreateNotificationMethodRequest;

public sealed record CreateConsoleMethodRequest : CreateNotificationMethodRequest
{
    public required string Name { get; init; }
}

public sealed record CreateNtfyMethodRequest : CreateNotificationMethodRequest
{
    public required string TopicName { get; init; }
}

public sealed record AddNotificationPreferenceRequest(
    int UserId,
    CreateNotificationMethodRequest MethodRequest) : IRequest<Result>;

public class AddNotificationPreferenceHandler(ChoreDbContext db, INotificationService notificationService)
    : IRequestHandler<AddNotificationPreferenceRequest, Result>
{
    public async Task<Result> Handle(AddNotificationPreferenceRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .Include(u => u.NotificationPreference)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
            return Result.Fail(new NotFoundError("User", request.UserId));

        Result<NotificationMethod> newNotificationMethod = request.MethodRequest switch
        {
            CreateConsoleMethodRequest consoleReq =>
                ConsoleMethod.Create(consoleReq.Name)
                    .Map(m => (NotificationMethod)m),

            CreateNtfyMethodRequest ntfyReq =>
                NtfyMethod.Create(ntfyReq.TopicName)
                    .Map(m => (NotificationMethod)m),

            _ => throw new InvalidOperationException(
                "Unknown notification method request type.") // Consider this programmer error
        };

        if (newNotificationMethod.IsFailed)
            return Result.Fail(newNotificationMethod.Errors);

        user.NotificationPreference = newNotificationMethod.Value;
        await db.SaveChangesAsync(cancellationToken);

        await notificationService.SendNotificationAsync(
            user.Id,
            "Notification Preference Updated",
            "Your notification preference has been updated.",
            cancellationToken);

        return Result.Ok();
    }
}
