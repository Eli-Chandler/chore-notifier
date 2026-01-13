using ChoreNotifier.Common;
using ChoreNotifier.Infrastructure.Notifications;
using ChoreNotifier.Models;
using FluentAssertions;
using FluentResults;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace ChoreNotifier.Tests.Infrastructure.Notifications;

[TestSubject(typeof(NotificationService))]
public class NotificationServiceTest : DatabaseTestBase
{
    public NotificationServiceTest(DatabaseFixture dbFixture, ClockFixture clockFixture) : base(dbFixture, clockFixture)
    {
    }

    private NotificationService CreateService(INotificationRouter? router = null)
    {
        router ??= new NotificationRouter(new[] { new TestNotificationSender(NotificationType.Console) });
        return new NotificationService(DbFixture.CreateDbContext(), router);
    }

    [Fact]
    public async Task SendNotificationAsync_WhenUserDoesNotExist_ReturnsNotFoundError()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.SendNotificationAsync(
            userId: 9999,
            title: "Test",
            message: "Test message");

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .BeOfType<NotFoundError>()
            .Which
            .Should()
            .Match<NotFoundError>(e =>
                e.EntityName == "User" &&
                (int)e.EntityKey == 9999);
    }

    [Fact]
    public async Task SendNotificationAsync_WhenUserHasNoNotificationPreference_FailsAndCreatesFailedAttempt()
    {
        // Arrange
        var user = await Factory.CreateUserAsync();
        var service = CreateService();

        // Act
        var result = await service.SendNotificationAsync(
            userId: user.Id,
            title: "Test Title",
            message: "Test Message");

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .BeOfType<InvalidOperationError>()
            .Which
            .Message.Should().Be("User has no notification preference set.");

        // Verify failed attempt was created
        using var context = DbFixture.CreateDbContext();
        var attempt = await context.NotificationAttempts
            .Include(a => a.Recipient)
            .FirstOrDefaultAsync(a => a.Recipient.Id == user.Id);

        attempt.Should().NotBeNull();
        attempt!.DeliveryStatus.Should().Be(DeliveryStatus.Failed);
        attempt.FailureReason.Should().Be("User has no notification preference set.");
        attempt.Notification.Title.Should().Be("Test Title");
        attempt.Notification.Message.Should().Be("Test Message");
        attempt.NotificationType.Should().BeNull();
    }

    [Fact]
    public async Task SendNotificationAsync_WhenSuccessful_CreatesDeliveredAttempt()
    {
        // Arrange
        var user = await Factory.CreateUserAsync();
        var consoleMethod = ConsoleMethod.Create("test-console").Value;

        using (var context = DbFixture.CreateDbContext())
        {
            var userFromDb = await context.Users.FindAsync(user.Id);
            userFromDb!.NotificationPreference = consoleMethod;
            await context.SaveChangesAsync();
        }

        var testSender = new TestNotificationSender(NotificationType.Console);
        var router = new NotificationRouter(new[] { testSender });
        var service = CreateService(router);

        // Act
        var result = await service.SendNotificationAsync(
            userId: user.Id,
            title: "Test Title",
            message: "Test Message");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        // Verify delivered attempt was created
        using var verifyContext = DbFixture.CreateDbContext();
        var attempt = await verifyContext.NotificationAttempts
            .Include(a => a.Recipient)
            .FirstOrDefaultAsync(a => a.Id == result.Value);

        attempt.Should().NotBeNull();
        attempt!.DeliveryStatus.Should().Be(DeliveryStatus.Delivered);
        attempt.DeliveredAt.Should().NotBeNull();
        attempt.DeliveredAt.Should().BeOnOrAfter(attempt.AttemptedAt);
        attempt.Notification.Title.Should().Be("Test Title");
        attempt.Notification.Message.Should().Be("Test Message");
        attempt.NotificationType.Should().Be(NotificationType.Console);

        // Verify the router actually sent the notification
        testSender.SentNotifications.Should().ContainSingle();
        testSender.SentNotifications[0].notification.Title.Should().Be("Test Title");
        testSender.SentNotifications[0].notification.Message.Should().Be("Test Message");
    }

    [Fact]
    public async Task SendNotificationAsync_WhenRouterFails_CreatesFailedAttempt()
    {
        // Arrange
        var user = await Factory.CreateUserAsync();
        var consoleMethod = ConsoleMethod.Create("test-console").Value;

        using (var context = DbFixture.CreateDbContext())
        {
            var userFromDb = await context.Users.FindAsync(user.Id);
            userFromDb!.NotificationPreference = consoleMethod;
            await context.SaveChangesAsync();
        }

        var failingSender = new TestNotificationSender(NotificationType.Console, shouldFail: true);
        var router = new NotificationRouter(new[] { failingSender });
        var service = CreateService(router);

        // Act
        var result = await service.SendNotificationAsync(
            userId: user.Id,
            title: "Test Title",
            message: "Test Message");

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Be("Test sender failed");

        // Verify failed attempt was created
        using var verifyContext = DbFixture.CreateDbContext();
        var attempt = await verifyContext.NotificationAttempts
            .Include(a => a.Recipient)
            .FirstOrDefaultAsync(a => a.Recipient.Id == user.Id);

        attempt.Should().NotBeNull();
        attempt!.DeliveryStatus.Should().Be(DeliveryStatus.Failed);
        attempt.FailureReason.Should().Be("Test sender failed");
        attempt.DeliveredAt.Should().BeNull();
    }

    [Fact]
    public async Task SendNotificationAsync_WithNtfyMethod_UsesCorrectNotificationType()
    {
        // Arrange
        var user = await Factory.CreateUserAsync();
        var ntfyMethod = NtfyMethod.Create("test-topic").Value;

        using (var context = DbFixture.CreateDbContext())
        {
            var userFromDb = await context.Users.FindAsync(user.Id);
            userFromDb!.NotificationPreference = ntfyMethod;
            await context.SaveChangesAsync();
        }

        var testSender = new TestNotificationSender(NotificationType.Ntfy);
        var router = new NotificationRouter(new[] { testSender });
        var service = CreateService(router);

        // Act
        var result = await service.SendNotificationAsync(
            userId: user.Id,
            title: "Ntfy Test",
            message: "Ntfy Message");

        // Assert
        result.IsSuccess.Should().BeTrue();

        using var verifyContext = DbFixture.CreateDbContext();
        var attempt = await verifyContext.NotificationAttempts
            .FirstOrDefaultAsync(a => a.Id == result.Value);

        attempt.Should().NotBeNull();
        attempt!.NotificationType.Should().Be(NotificationType.Ntfy);
        attempt.DeliveryStatus.Should().Be(DeliveryStatus.Delivered);
    }
}
