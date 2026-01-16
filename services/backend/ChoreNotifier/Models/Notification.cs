using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using FluentResults;

namespace ChoreNotifier.Models;

public record Notification(string Title, string Message);

public enum NotificationType
{
    Console,
    Ntfy
}

public abstract class NotificationMethod
{
    public int Id { get; private set; }
    public NotificationType Type { get; private set; }

    protected NotificationMethod(NotificationType type)
    {
        Type = type;
    }
}

public class ConsoleMethod : NotificationMethod
{
    public string Name { get; private set; } = null!;

    private ConsoleMethod() : base(NotificationType.Console)
    {
    } // EF

    private ConsoleMethod(string name) : base(NotificationType.Console)
    {
        Name = name;
    }

    public static Result<ConsoleMethod> Create(string name)
    {
        if (name.Trim().Length == 0)
            return Result.Fail<ConsoleMethod>("Console method name cannot be empty.");
        return new ConsoleMethod(name.Trim());
    }
}

public class NtfyMethod : NotificationMethod
{
    private static readonly Regex TopicRegex =
        new(@"^[-_A-Za-z0-9]{1,64}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    public string TopicName { get; private set; } = null!;

    private NtfyMethod() : base(NotificationType.Ntfy)
    {
    } // EF

    private NtfyMethod(string topicName) : base(NotificationType.Ntfy)
    {
        TopicName = topicName;
    }

    public static Result<NtfyMethod> Create(string topicName)
    {
        if (!TopicRegex.IsMatch(topicName))
            return Result.Fail<NtfyMethod>(
                "Topic name must be 1-64 characters long and can only contain letters, numbers, hyphens, and underscores.");
        return new NtfyMethod(topicName);
    }
}

public enum DeliveryStatus
{
    Pending,
    Delivered,
    Failed
}

public class NotificationAttempt
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public required Notification Notification { get; init; }
    public required NotificationType? NotificationType { get; init; }
    public required DateTimeOffset AttemptedAt { get; init; }

    public required User Recipient { get; init; }

    public DeliveryStatus DeliveryStatus { get; private set; } = DeliveryStatus.Pending;
    public DateTimeOffset? DeliveredAt { get; private set; }
    [MaxLength(1000)] public string? FailureReason { get; private set; }

    public void MarkDelivered(DateTimeOffset deliveredAt)
    {
        if (DeliveryStatus == DeliveryStatus.Failed)
            throw new InvalidOperationException("Cannot mark delivered; attempt is already marked as failed.");

        if (DeliveryStatus == DeliveryStatus.Delivered)
            throw new InvalidOperationException("Cannot mark delivered; attempt is already marked as delivered.");

        if (deliveredAt < AttemptedAt)
            throw new ArgumentOutOfRangeException(nameof(deliveredAt),
                "DeliveredAt cannot be earlier than AttemptedAt.");

        DeliveryStatus = DeliveryStatus.Delivered;
        DeliveredAt = deliveredAt;
    }

    public void MarkFailed(string reason)
    {
        if (DeliveryStatus == DeliveryStatus.Delivered)
            throw new InvalidOperationException("Cannot mark failed; attempt is already marked as delivered.");

        if (DeliveryStatus == DeliveryStatus.Failed)
            throw new InvalidOperationException("Cannot mark failed; attempt is already marked as failed.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Failure reason is required.", nameof(reason));

        DeliveryStatus = DeliveryStatus.Failed;
        // FailureReason = reason.Trim();
        var trimmedReason = reason.Trim();
        FailureReason = trimmedReason.Length <= 1000 ? trimmedReason : trimmedReason.Substring(0, 997) + "...";
    }
}
