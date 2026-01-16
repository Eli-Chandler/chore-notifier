using ChoreNotifier.Common;
using ChoreNotifier.Data;
using ChoreNotifier.Models;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ChoreNotifier.Features.Notifications.ListNotificationHistory;

public sealed record ListNotificationHistoryRequest(
    int UserId,
    int PageSize,
    DateTimeOffset? AfterDate = null)
    : IRequest<Result<KeysetPage<ListNotificationHistoryResponseItem, DateTimeOffset>>>;

public sealed record ListNotificationHistoryResponseItem(
    Guid Id,
    string Title,
    string Message,
    NotificationType? NotificationType,
    DateTimeOffset AttemptedAt,
    DeliveryStatus DeliveryStatus,
    DateTimeOffset? DeliveredAt,
    string? FailureReason
);

public class ListNotificationHistoryHandler(ChoreDbContext db)
    : IRequestHandler<ListNotificationHistoryRequest, Result<KeysetPage<ListNotificationHistoryResponseItem, DateTimeOffset>>>
{
    public async Task<Result<KeysetPage<ListNotificationHistoryResponseItem, DateTimeOffset>>> Handle(
        ListNotificationHistoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var validateResult = ValidatePageSize(request.PageSize);
        if (validateResult.IsFailed)
            return validateResult.ToResult<KeysetPage<ListNotificationHistoryResponseItem, DateTimeOffset>>();

        var userExists = await db.Users.AnyAsync(u => u.Id == request.UserId, cancellationToken);
        if (!userExists)
            return Result.Fail(new NotFoundError("User", request.UserId));

        var result = await db.NotificationAttempts
            .Where(na => na.Recipient.Id == request.UserId)
            .OrderByDescending(na => na.AttemptedAt)
            .Where(na => request.AfterDate == null || na.AttemptedAt < request.AfterDate.Value)
            .ToKeysetPageAsync(request.PageSize, na => na.AttemptedAt, cancellationToken);

        return result.Select(na => new ListNotificationHistoryResponseItem(
            na.Id,
            na.Notification.Title,
            na.Notification.Message,
            na.NotificationType,
            na.AttemptedAt,
            na.DeliveryStatus,
            na.DeliveredAt,
            na.FailureReason
        ));
    }

    private static Result ValidatePageSize(int pageSize)
    {
        if (pageSize <= 0)
            return Result.Fail(new ValidationError("Page size must be greater than 0"));
        if (pageSize > 100)
            return Result.Fail(new ValidationError("Page size cannot exceed 100"));
        return Result.Ok();
    }
}
