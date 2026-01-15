using ChoreNotifier.Models;
using FluentResults;

namespace ChoreNotifier.Infrastructure.Notifications;

public interface INotificationService
{
    Task<Result<Guid>> SendNotificationAsync(
        int userId,
        string title,
        string message,
        CancellationToken cancellationToken = default);
}
