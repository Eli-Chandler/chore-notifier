using FluentResults;

namespace ChoreNotifier.Infrastructure.Notifications;

public abstract record NotificationMethod(string Type, bool IsEnabled);
// public sealed record EmailMethod(string Address, bool IsEnabled)
//     : NotificationMethod("email", IsEnabled);
public sealed record PushOverMethod(string Token, string UserKey, bool IsEnabled)
    : NotificationMethod("pushover", IsEnabled);


public sealed record Notification(string Title, string Message);

public interface INotificationSender<in TMethod> where TMethod : NotificationMethod
{
    Task<Result> SendNotificationAsync(Notification notification, TMethod method);
}
