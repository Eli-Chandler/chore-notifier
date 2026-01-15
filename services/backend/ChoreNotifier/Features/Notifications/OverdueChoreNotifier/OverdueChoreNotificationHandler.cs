using ChoreNotifier.Data;
using ChoreNotifier.Infrastructure.Clock;
using ChoreNotifier.Infrastructure.Notifications;
using Microsoft.EntityFrameworkCore;

namespace ChoreNotifier.Features.Notifications.OverdueChoreNotifier;

public class OverdueChoreNotificationHandler
{
    private readonly ChoreDbContext _db;
    private readonly INotificationService _notificationService;
    private readonly IClock _clock;
    private readonly ILogger<OverdueChoreNotificationHandler> _logger;
    private const int NotificationCooldownHours = 1;

    public OverdueChoreNotificationHandler(
        ChoreDbContext db,
        INotificationService notificationService,
        IClock clock,
        ILogger<OverdueChoreNotificationHandler> logger)
    {
        _db = db;
        _notificationService = notificationService;
        _clock = clock;
        _logger = logger;
    }

    public async Task CheckAndNotifyOverdueChoresAsync(CancellationToken cancellationToken = default)
    {
        var currentTime = _clock.UtcNow;

        // Find all incomplete chore occurrences that are overdue and were not recently notified
        var overdueOccurrences = await _db.ChoreOccurrences
            .Include(co => co.Chore)
            .Include(co => co.User)
            .Where(co => co.CompletedAt == null && co.DueAt <= currentTime)
            .Where(co => co.LastNotifiedAt == null || co.LastNotifiedAt <= currentTime.AddHours(-NotificationCooldownHours))
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Found {Count} overdue chore occurrence(s)", overdueOccurrences.Count);

        foreach (var occurrence in overdueOccurrences)
        {
            try
            {
                var title = $"Chore Overdue: {occurrence.Chore.Title}";
                var message = $"Your chore '{occurrence.Chore.Title}' was due at {occurrence.DueAt:g}. Please complete it as soon as possible.";

                var result = await _notificationService.SendNotificationAsync(
                    occurrence.User.Id,
                    title,
                    message,
                    cancellationToken);

                if (result.IsSuccess)
                {
                    occurrence.UpdateLastNotifiedAt(currentTime);
                    await _db.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation(
                        "Sent notification for overdue chore '{ChoreTitle}' (Occurrence ID: {OccurrenceId}) to user {UserId}",
                        occurrence.Chore.Title,
                        occurrence.Id,
                        occurrence.User.Id);
                }
                else
                {
                    _logger.LogWarning(
                        "Failed to send notification for overdue chore '{ChoreTitle}' (Occurrence ID: {OccurrenceId}): {Errors}",
                        occurrence.Chore.Title,
                        occurrence.Id,
                        string.Join("; ", result.Errors.Select(e => e.Message)));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error sending notification for overdue chore occurrence {OccurrenceId}",
                    occurrence.Id);
            }
        }
    }
}
