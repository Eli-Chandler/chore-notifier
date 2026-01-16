using ChoreNotifier.Common;
using ChoreNotifier.Data;
using ChoreNotifier.Models;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace ChoreNotifier.Infrastructure.Notifications;

public class NotificationService : INotificationService
{
    private readonly ChoreDbContext _db;
    private readonly INotificationRouter _router;

    public NotificationService(ChoreDbContext db, INotificationRouter router)
    {
        _db = db;
        _router = router;
    }

    public async Task<Result<Guid>> SendNotificationAsync(
        int userId,
        string title,
        string message,
        CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.Include(u => u.NotificationPreference)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null)
            return Result.Fail<Guid>(new NotFoundError("User", userId));

        var notification = new Notification(title, message);

        if (user.NotificationPreference == null)
        {
            var failedAttempt = new NotificationAttempt
            {
                Notification = notification,
                NotificationType = null,
                AttemptedAt = DateTimeOffset.UtcNow,
                Recipient = user
            };

            _db.NotificationAttempts.Add(failedAttempt);
            failedAttempt.MarkFailed("User has no notification preference set.");
            await _db.SaveChangesAsync(cancellationToken);
            return Result.Fail<Guid>(new InvalidOperationError("User has no notification preference set."));
        }

        var attempt = new NotificationAttempt
        {
            Notification = notification,
            NotificationType = user.NotificationPreference.Type,
            AttemptedAt = DateTimeOffset.UtcNow,
            Recipient = user
        };

        _db.NotificationAttempts.Add(attempt);

        var sendResult = await _router.SendAsync(notification, user.NotificationPreference);

        if (sendResult.IsSuccess)
        {
            attempt.MarkDelivered(DateTimeOffset.UtcNow);
            await _db.SaveChangesAsync(cancellationToken);
            return Result.Ok(attempt.Id);
        }

        var failureReason = string.Join("; ", sendResult.Errors.Select(e => e.Message));
        attempt.MarkFailed(failureReason);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Fail<Guid>(sendResult.Errors);
    }
}
