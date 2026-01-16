using ChoreNotifier.Infrastructure.Notifications;
using ChoreNotifier.Models;
using FluentAssertions;
using FluentResults;
using JetBrains.Annotations;

namespace ChoreNotifier.Tests.Infrastructure.Notifications;

[TestSubject(typeof(NotificationRouter))]
public class NotificationRouterTest
{
    [Fact]
    public async Task SendAsync_WhenMethodNotConfigured_Throws()
    {
        // Arrange
        var router = new NotificationRouter(new List<INotificationSender>());
        var notification = new Notification("Test", "Message");
        var method = ConsoleMethod.Create("test").Value;

        // Act
        var act = async () => await router.SendAsync(notification, method);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("No sender configured for notification type Console");
    }

    [Theory]
    [InlineData(NotificationType.Console)]
    [InlineData(NotificationType.Ntfy)]
    public async Task SendAsync_WithConfiguredSender_RoutesSuccessfully(NotificationType notificationType)
    {
        // Arrange
        var testSender = new TestNotificationSender(notificationType);
        var router = new NotificationRouter(new[] { testSender });
        var notification = new Notification("Test Title", "Test Message");
        Result<NotificationMethod> createMethodResult = notificationType switch
        {
            NotificationType.Console => ConsoleMethod.Create("test-console").Map(m => (NotificationMethod)m),
            NotificationType.Ntfy => NtfyMethod.Create("test-topic").Map(m => (NotificationMethod)m),
            _ => throw new ArgumentOutOfRangeException(nameof(notificationType))
        };

        if (createMethodResult.IsFailed)
            throw new InvalidOperationException("Failed to create notification method for test.");

        var method = createMethodResult.Value;

        // Act
        var result = await router.SendAsync(notification, method);

        // Assert
        result.IsSuccess.Should().BeTrue();
        testSender.SentNotifications.Should().ContainSingle();
        testSender.SentNotifications[0].notification.Should().Be(notification);
        testSender.SentNotifications[0].method.Should().Be(method);
    }

    [Fact]
    public async Task SendAsync_WithMultipleSenders_RoutesToCorrectSender()
    {
        // Arrange
        var consoleSender = new TestNotificationSender(NotificationType.Console);
        var ntfySender = new TestNotificationSender(NotificationType.Ntfy);
        var router = new NotificationRouter(new INotificationSender[] { consoleSender, ntfySender });

        var notification = new Notification("Test", "Message");
        var ntfyMethod = NtfyMethod.Create("test-topic").Value;

        // Act
        var result = await router.SendAsync(notification, ntfyMethod);

        // Assert
        result.IsSuccess.Should().BeTrue();
        ntfySender.SentNotifications.Should().ContainSingle();
        consoleSender.SentNotifications.Should().BeEmpty();
    }

    [Fact]
    public async Task SendAsync_WhenSenderFails_ReturnsFailure()
    {
        // Arrange
        var failingSender = new TestNotificationSender(NotificationType.Console, shouldFail: true);
        var router = new NotificationRouter(new[] { failingSender });
        var notification = new Notification("Test", "Message");
        var method = ConsoleMethod.Create("test").Value;

        // Act
        var result = await router.SendAsync(notification, method);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Be("Test sender failed");
    }
}

internal class TestNotificationSender : INotificationSender
{
    private readonly bool _shouldFail;

    public NotificationType Type { get; }
    public List<(Notification notification, NotificationMethod method)> SentNotifications { get; } = new();

    public TestNotificationSender(NotificationType type, bool shouldFail = false)
    {
        Type = type;
        _shouldFail = shouldFail;
    }

    public Task<Result> SendNotificationAsync(Notification notification, NotificationMethod method)
    {
        if (_shouldFail)
            return Task.FromResult(Result.Fail("Test sender failed"));

        SentNotifications.Add((notification, method));
        return Task.FromResult(Result.Ok());
    }
}
