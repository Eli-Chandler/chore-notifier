using System.Text.Json.Serialization;
using ChoreNotifier.Data;
using ChoreNotifier.Models;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ChoreNotifier.Features.Notifications.GetNotificationPreference;

public sealed record GetNotificationPreferenceRequest(int UserId) : IRequest<Result<GetNotificationPreferenceResponse?>>;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ConsoleMethodResponse), "Console")]
[JsonDerivedType(typeof(NtfyMethodResponse), "Ntfy")]
public abstract record GetNotificationPreferenceResponse;

public sealed record ConsoleMethodResponse(string Name) : GetNotificationPreferenceResponse;

public sealed record NtfyMethodResponse(string TopicName) : GetNotificationPreferenceResponse;

public sealed class GetNotificationPreferenceHandler(ChoreDbContext db)
    : IRequestHandler<GetNotificationPreferenceRequest, Result<GetNotificationPreferenceResponse?>>
{
    public async Task<Result<GetNotificationPreferenceResponse?>> Handle(
        GetNotificationPreferenceRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await db.Users
            .Include(u => u.NotificationPreference)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
            return Result.Fail(new NotFoundError("User", request.UserId));

        if (user.NotificationPreference is null)
            return Result.Ok<GetNotificationPreferenceResponse?>(null);

        GetNotificationPreferenceResponse response = user.NotificationPreference switch
        {
            ConsoleMethod console => new ConsoleMethodResponse(console.Name),
            NtfyMethod ntfy => new NtfyMethodResponse(ntfy.TopicName),
            _ => throw new InvalidOperationException("Unknown notification method type.")
        };

        return Result.Ok<GetNotificationPreferenceResponse?>(response);
    }
}
