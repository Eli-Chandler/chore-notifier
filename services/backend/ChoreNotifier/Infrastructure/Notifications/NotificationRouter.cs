using ChoreNotifier.Models;
using FluentResults;

namespace ChoreNotifier.Infrastructure.Notifications;

public interface INotificationRouter
{
    Task<Result> SendAsync(Notification notification, NotificationMethod method);
}

public class NotificationRouter : INotificationRouter
{
    private readonly Dictionary<NotificationType, INotificationSender> _senders;

    public NotificationRouter(IEnumerable<INotificationSender> senders)
    {
        _senders = senders.ToDictionary(s => s.Type);
    }

    public async Task<Result> SendAsync(Notification notification, NotificationMethod method)
    {
        if (!_senders.TryGetValue(method.Type, out var sender))
            throw new InvalidOperationException($"No sender configured for notification type {method.Type}");

        return await sender.SendNotificationAsync(notification, method);
    }
}
