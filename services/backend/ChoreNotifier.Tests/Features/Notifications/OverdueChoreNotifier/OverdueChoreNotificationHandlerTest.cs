using ChoreNotifier.Features.Notifications.OverdueChoreNotifier;
using ChoreNotifier.Infrastructure.Notifications;
using ChoreNotifier.Models;
using FluentAssertions;
using FluentResults;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ChoreNotifier.Tests.Features.Notifications.OverdueChoreNotifier;

[TestSubject(typeof(OverdueChoreNotificationHandler))]
public class OverdueChoreNotificationHandlerTest : DatabaseTestBase
{
    private readonly Mock<INotificationRouter> _routerMock = new();

    public OverdueChoreNotificationHandlerTest(DatabaseFixture dbFixture, ClockFixture clockFixture) : base(dbFixture, clockFixture)
    {
        _routerMock
            .Setup(r => r.SendAsync(It.IsAny<Notification>(), It.IsAny<NotificationMethod>()))
            .ReturnsAsync(Result.Ok());
    }

    private OverdueChoreNotificationHandler CreateHandler()
    {
        var notificationService = new NotificationService(DbFixture.CreateDbContext(), _routerMock.Object);
        return new OverdueChoreNotificationHandler(
            DbFixture.CreateDbContext(),
            notificationService,
            TestClock,
            NullLogger<OverdueChoreNotificationHandler>.Instance);
    }

    private async Task<User> CreateUserWithNotificationPreferenceAsync()
    {
        await using var context = DbFixture.CreateDbContext();
        var user = await Factory.CreateUserAsync(context: context);
        user.NotificationPreference = NtfyMethod.Create("test-topic").Value;
        await context.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task CheckAndNotifyOverdueChoresAsync_WhenNoOverdueChores_SendsNoNotifications()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        await handler.CheckAndNotifyOverdueChoresAsync();

        // Assert - router was never called
        _routerMock.Verify(
            r => r.SendAsync(It.IsAny<Notification>(), It.IsAny<NotificationMethod>()),
            Times.Never);

        // Assert - no NotificationAttempts were created
        await using var context = DbFixture.CreateDbContext();
        var attempts = await context.NotificationAttempts.ToListAsync();
        attempts.Should().BeEmpty();
    }

    [Fact]
    public async Task CheckAndNotifyOverdueChoresAsync_WhenChoreIsOverdueAndNeverNotified_SendsNotification()
    {
        // Arrange
        var user = await CreateUserWithNotificationPreferenceAsync();
        var now = DateTimeOffset.UtcNow;
        TestClock.SetTime(now);

        int occurrenceId;
        await using (var context = DbFixture.CreateDbContext())
        {
            var chore = await Factory.CreateChoreAsync(title: "Take out trash", context: context);
            chore.AddAssignee(user);
            await context.SaveChangesAsync();

            // Create occurrence that was due 1 hour ago
            var occurrence = new ChoreOccurrence(chore, user, now.AddHours(-1));
            context.ChoreOccurrences.Add(occurrence);
            await context.SaveChangesAsync();
            occurrenceId = occurrence.Id;
        }

        var handler = CreateHandler();

        // Act
        await handler.CheckAndNotifyOverdueChoresAsync();

        // Assert - router was called with correct notification
        _routerMock.Verify(
            r => r.SendAsync(
                It.Is<Notification>(n => n.Title.Contains("Take out trash")),
                It.IsAny<NotificationMethod>()),
            Times.Once);

        // Assert - NotificationAttempt was created
        await using (var context = DbFixture.CreateDbContext())
        {
            var attempt = await context.NotificationAttempts
                .FirstOrDefaultAsync(a => a.Recipient.Id == user.Id);
            attempt.Should().NotBeNull();
            attempt!.DeliveryStatus.Should().Be(DeliveryStatus.Delivered);
            attempt.Notification.Title.Should().Contain("Take out trash");
        }

        // Assert - LastNotifiedAt was updated
        await using (var verifyContext = DbFixture.CreateDbContext())
        {
            var occurrence = await verifyContext.ChoreOccurrences.FindAsync(occurrenceId);
            occurrence!.LastNotifiedAt.Should().NotBeNull();
            occurrence.LastNotifiedAt.Should().BeCloseTo(now, TimeSpan.FromMicroseconds(1));
        }
    }

    [Fact]
    public async Task CheckAndNotifyOverdueChoresAsync_WhenChoreWasRecentlyNotified_DoesNotSendNotification()
    {
        // Arrange
        var user = await CreateUserWithNotificationPreferenceAsync();
        var now = DateTimeOffset.UtcNow;
        var lastNotifiedAt = now.AddMinutes(-30);
        TestClock.SetTime(now);

        int occurrenceId;
        await using (var context = DbFixture.CreateDbContext())
        {
            var chore = await Factory.CreateChoreAsync(context: context);
            chore.AddAssignee(user);
            await context.SaveChangesAsync();

            // Create occurrence that was due 2 hours ago but notified 30 minutes ago (within cooldown)
            var occurrence = new ChoreOccurrence(chore, user, now.AddHours(-2));
            occurrence.UpdateLastNotifiedAt(lastNotifiedAt);
            context.ChoreOccurrences.Add(occurrence);
            await context.SaveChangesAsync();
            occurrenceId = occurrence.Id;
        }

        var handler = CreateHandler();

        // Act
        await handler.CheckAndNotifyOverdueChoresAsync();

        // Assert - router was never called
        _routerMock.Verify(
            r => r.SendAsync(It.IsAny<Notification>(), It.IsAny<NotificationMethod>()),
            Times.Never);

        // Assert - no NotificationAttempts were created
        await using (var context = DbFixture.CreateDbContext())
        {
            var attempts = await context.NotificationAttempts.ToListAsync();
            attempts.Should().BeEmpty();
        }

        // Assert - LastNotifiedAt was NOT changed
        await using (var verifyContext = DbFixture.CreateDbContext())
        {
            var occurrence = await verifyContext.ChoreOccurrences.FindAsync(occurrenceId);
            occurrence!.LastNotifiedAt.Should().BeCloseTo(lastNotifiedAt, TimeSpan.FromMicroseconds(1));
        }
    }

    [Fact]
    public async Task CheckAndNotifyOverdueChoresAsync_WhenChoreWasNotifiedOverOneHourAgo_SendsNotification()
    {
        // Arrange
        var user = await CreateUserWithNotificationPreferenceAsync();
        var now = DateTimeOffset.UtcNow;
        TestClock.SetTime(now);

        int occurrenceId;
        await using (var context = DbFixture.CreateDbContext())
        {
            var chore = await Factory.CreateChoreAsync(title: "Wash dishes", context: context);
            chore.AddAssignee(user);
            await context.SaveChangesAsync();

            // Create occurrence that was due 3 hours ago and notified 2 hours ago (outside cooldown)
            var occurrence = new ChoreOccurrence(chore, user, now.AddHours(-3));
            occurrence.UpdateLastNotifiedAt(now.AddHours(-2));
            context.ChoreOccurrences.Add(occurrence);
            await context.SaveChangesAsync();
            occurrenceId = occurrence.Id;
        }

        var handler = CreateHandler();

        // Act
        await handler.CheckAndNotifyOverdueChoresAsync();

        // Assert - router was called with correct notification
        _routerMock.Verify(
            r => r.SendAsync(
                It.Is<Notification>(n => n.Title.Contains("Wash dishes")),
                It.IsAny<NotificationMethod>()),
            Times.Once);

        // Assert - NotificationAttempt was created
        await using (var context = DbFixture.CreateDbContext())
        {
            var attempt = await context.NotificationAttempts
                .FirstOrDefaultAsync(a => a.Recipient.Id == user.Id);
            attempt.Should().NotBeNull();
            attempt!.DeliveryStatus.Should().Be(DeliveryStatus.Delivered);
            attempt.Notification.Title.Should().Contain("Wash dishes");
        }

        // Assert - LastNotifiedAt was updated to current time
        await using (var verifyContext = DbFixture.CreateDbContext())
        {
            var occurrence = await verifyContext.ChoreOccurrences.FindAsync(occurrenceId);
            occurrence!.LastNotifiedAt.Should().BeCloseTo(now, TimeSpan.FromMicroseconds(1));
        }
    }

    [Fact]
    public async Task CheckAndNotifyOverdueChoresAsync_WhenChoreIsCompleted_DoesNotSendNotification()
    {
        // Arrange
        var user = await CreateUserWithNotificationPreferenceAsync();
        var now = DateTimeOffset.UtcNow;
        TestClock.SetTime(now);

        int occurrenceId;
        await using (var context = DbFixture.CreateDbContext())
        {
            var chore = await Factory.CreateChoreAsync(context: context);
            chore.AddAssignee(user);
            await context.SaveChangesAsync();

            // Create occurrence that was due 1 hour ago but is completed
            var occurrence = new ChoreOccurrence(chore, user, now.AddHours(-1));
            occurrence.Complete(user.Id, now.AddMinutes(-30));
            context.ChoreOccurrences.Add(occurrence);
            await context.SaveChangesAsync();
            occurrenceId = occurrence.Id;
        }

        var handler = CreateHandler();

        // Act
        await handler.CheckAndNotifyOverdueChoresAsync();

        // Assert - router was never called
        _routerMock.Verify(
            r => r.SendAsync(It.IsAny<Notification>(), It.IsAny<NotificationMethod>()),
            Times.Never);

        // Assert - no NotificationAttempts were created
        await using (var context = DbFixture.CreateDbContext())
        {
            var attempts = await context.NotificationAttempts.ToListAsync();
            attempts.Should().BeEmpty();
        }

        // Assert - LastNotifiedAt remains null
        await using (var verifyContext = DbFixture.CreateDbContext())
        {
            var occurrence = await verifyContext.ChoreOccurrences.FindAsync(occurrenceId);
            occurrence!.LastNotifiedAt.Should().BeNull();
        }
    }

    [Fact]
    public async Task CheckAndNotifyOverdueChoresAsync_WhenChoreIsNotYetDue_DoesNotSendNotification()
    {
        // Arrange
        var user = await CreateUserWithNotificationPreferenceAsync();
        var now = DateTimeOffset.UtcNow;
        TestClock.SetTime(now);

        int occurrenceId;
        await using (var context = DbFixture.CreateDbContext())
        {
            var chore = await Factory.CreateChoreAsync(context: context);
            chore.AddAssignee(user);
            await context.SaveChangesAsync();

            // Create occurrence that is due in the future
            var occurrence = new ChoreOccurrence(chore, user, now.AddHours(1));
            context.ChoreOccurrences.Add(occurrence);
            await context.SaveChangesAsync();
            occurrenceId = occurrence.Id;
        }

        var handler = CreateHandler();

        // Act
        await handler.CheckAndNotifyOverdueChoresAsync();

        // Assert - router was never called
        _routerMock.Verify(
            r => r.SendAsync(It.IsAny<Notification>(), It.IsAny<NotificationMethod>()),
            Times.Never);

        // Assert - no NotificationAttempts were created
        await using (var context = DbFixture.CreateDbContext())
        {
            var attempts = await context.NotificationAttempts.ToListAsync();
            attempts.Should().BeEmpty();
        }

        // Assert - LastNotifiedAt remains null
        await using (var verifyContext = DbFixture.CreateDbContext())
        {
            var occurrence = await verifyContext.ChoreOccurrences.FindAsync(occurrenceId);
            occurrence!.LastNotifiedAt.Should().BeNull();
        }
    }

    [Fact]
    public async Task CheckAndNotifyOverdueChoresAsync_WithMultipleOverdueChores_SendsNotificationForEach()
    {
        // Arrange
        var user = await CreateUserWithNotificationPreferenceAsync();
        var now = DateTimeOffset.UtcNow;
        TestClock.SetTime(now);

        int occurrence1Id, occurrence2Id;
        await using (var context = DbFixture.CreateDbContext())
        {
            var chore1 = await Factory.CreateChoreAsync(title: "Chore 1", context: context);
            var chore2 = await Factory.CreateChoreAsync(title: "Chore 2", context: context);
            chore1.AddAssignee(user);
            chore2.AddAssignee(user);
            await context.SaveChangesAsync();

            var occurrence1 = new ChoreOccurrence(chore1, user, now.AddHours(-1));
            var occurrence2 = new ChoreOccurrence(chore2, user, now.AddHours(-2));
            context.ChoreOccurrences.Add(occurrence1);
            context.ChoreOccurrences.Add(occurrence2);
            await context.SaveChangesAsync();
            occurrence1Id = occurrence1.Id;
            occurrence2Id = occurrence2.Id;
        }

        var handler = CreateHandler();

        // Act
        await handler.CheckAndNotifyOverdueChoresAsync();

        // Assert - router was called for each chore
        _routerMock.Verify(
            r => r.SendAsync(It.Is<Notification>(n => n.Title.Contains("Chore 1")), It.IsAny<NotificationMethod>()),
            Times.Once);
        _routerMock.Verify(
            r => r.SendAsync(It.Is<Notification>(n => n.Title.Contains("Chore 2")), It.IsAny<NotificationMethod>()),
            Times.Once);

        // Assert - NotificationAttempts were created for both
        await using (var context = DbFixture.CreateDbContext())
        {
            var attempts = await context.NotificationAttempts
                .Where(a => a.Recipient.Id == user.Id)
                .ToListAsync();
            attempts.Should().HaveCount(2);
            attempts.Should().AllSatisfy(a => a.DeliveryStatus.Should().Be(DeliveryStatus.Delivered));
            attempts.Select(a => a.Notification.Title).Should().Contain(t => t.Contains("Chore 1"));
            attempts.Select(a => a.Notification.Title).Should().Contain(t => t.Contains("Chore 2"));
        }

        // Assert - LastNotifiedAt was updated for both occurrences
        await using (var verifyContext = DbFixture.CreateDbContext())
        {
            var occurrence1 = await verifyContext.ChoreOccurrences.FindAsync(occurrence1Id);
            var occurrence2 = await verifyContext.ChoreOccurrences.FindAsync(occurrence2Id);
            occurrence1!.LastNotifiedAt.Should().BeCloseTo(now, TimeSpan.FromMicroseconds(1));
            occurrence2!.LastNotifiedAt.Should().BeCloseTo(now, TimeSpan.FromMicroseconds(1));
        }
    }

    [Fact]
    public async Task CheckAndNotifyOverdueChoresAsync_WhenNotificationFails_DoesNotUpdateLastNotifiedAt()
    {
        // Arrange
        var user = await CreateUserWithNotificationPreferenceAsync();
        var now = DateTimeOffset.UtcNow;
        TestClock.SetTime(now);

        _routerMock
            .Setup(r => r.SendAsync(It.IsAny<Notification>(), It.IsAny<NotificationMethod>()))
            .ReturnsAsync(Result.Fail("Notification failed"));

        int occurrenceId;
        await using (var context = DbFixture.CreateDbContext())
        {
            var chore = await Factory.CreateChoreAsync(title: "Failed chore", context: context);
            chore.AddAssignee(user);
            await context.SaveChangesAsync();

            var occurrence = new ChoreOccurrence(chore, user, now.AddHours(-1));
            context.ChoreOccurrences.Add(occurrence);
            await context.SaveChangesAsync();
            occurrenceId = occurrence.Id;
        }

        var handler = CreateHandler();

        // Act
        await handler.CheckAndNotifyOverdueChoresAsync();

        // Assert - router was called
        _routerMock.Verify(
            r => r.SendAsync(
                It.Is<Notification>(n => n.Title.Contains("Failed chore")),
                It.IsAny<NotificationMethod>()),
            Times.Once);

        // Assert - NotificationAttempt was created with Failed status
        await using (var context = DbFixture.CreateDbContext())
        {
            var attempt = await context.NotificationAttempts
                .FirstOrDefaultAsync(a => a.Recipient.Id == user.Id);
            attempt.Should().NotBeNull();
            attempt!.DeliveryStatus.Should().Be(DeliveryStatus.Failed);
            attempt.FailureReason.Should().Be("Notification failed");
            attempt.Notification.Title.Should().Contain("Failed chore");
        }

        // Assert - LastNotifiedAt was NOT updated
        await using (var verifyContext = DbFixture.CreateDbContext())
        {
            var occurrence = await verifyContext.ChoreOccurrences.FindAsync(occurrenceId);
            occurrence!.LastNotifiedAt.Should().BeNull();
        }
    }

    [Fact]
    public async Task CheckAndNotifyOverdueChoresAsync_WhenChoreIsSnoozed_DoesNotSendNotificationUntilSnoozeExpires()
    {
        // Arrange
        var user = await CreateUserWithNotificationPreferenceAsync();
        var now = DateTimeOffset.UtcNow;
        TestClock.SetTime(now);

        int occurrenceId;
        await using (var context = DbFixture.CreateDbContext())
        {
            var chore = await Factory.CreateChoreAsync(title: "Snoozeable chore", context: context);
            chore.AddAssignee(user);
            await context.SaveChangesAsync();

            // Create occurrence that was due 1 hour ago
            var occurrence = new ChoreOccurrence(chore, user, now.AddHours(-1));
            context.ChoreOccurrences.Add(occurrence);
            await context.SaveChangesAsync();
            occurrenceId = occurrence.Id;
        }

        var handler = CreateHandler();

        // Act 1 - First check should send a notification
        await handler.CheckAndNotifyOverdueChoresAsync();

        // Assert 1 - notification was sent
        _routerMock.Verify(
            r => r.SendAsync(
                It.Is<Notification>(n => n.Title.Contains("Snoozeable chore")),
                It.IsAny<NotificationMethod>()),
            Times.Once);

        // Arrange 2 - Snooze the chore for 1 hour
        var snoozeTime = now.AddMinutes(5);
        TestClock.SetTime(snoozeTime);
        var snoozeDuration = TimeSpan.FromHours(1);

        await using (var context = DbFixture.CreateDbContext())
        {
            var occurrence = await context.ChoreOccurrences
                .Include(o => o.Chore)
                .Include(o => o.User)
                .FirstAsync(o => o.Id == occurrenceId);
            var snoozeResult = occurrence.Snooze(user.Id, snoozeTime, snoozeDuration);
            snoozeResult.IsSuccess.Should().BeTrue();
            await context.SaveChangesAsync();
        }

        // Reset mock to track new calls
        _routerMock.Invocations.Clear();

        // Act 2 - Check again while snoozed (30 minutes after snooze)
        var duringSnoozeTime = snoozeTime.AddMinutes(30);
        TestClock.SetTime(duringSnoozeTime);
        handler = CreateHandler();
        await handler.CheckAndNotifyOverdueChoresAsync();

        // Assert 2 - no notification sent while snoozed
        _routerMock.Verify(
            r => r.SendAsync(It.IsAny<Notification>(), It.IsAny<NotificationMethod>()),
            Times.Never);

        // Act 3 - Check again after snooze expires
        var afterSnoozeTime = snoozeTime.Add(snoozeDuration).AddMinutes(5);
        TestClock.SetTime(afterSnoozeTime);
        handler = CreateHandler();
        await handler.CheckAndNotifyOverdueChoresAsync();

        // Assert 3 - notification sent after snooze expires
        _routerMock.Verify(
            r => r.SendAsync(
                It.Is<Notification>(n => n.Title.Contains("Snoozeable chore")),
                It.IsAny<NotificationMethod>()),
            Times.Once);

        // Assert - LastNotifiedAt was updated to the post-snooze time
        await using (var verifyContext = DbFixture.CreateDbContext())
        {
            var occurrence = await verifyContext.ChoreOccurrences.FindAsync(occurrenceId);
            occurrence!.LastNotifiedAt.Should().BeCloseTo(afterSnoozeTime, TimeSpan.FromMicroseconds(1));
        }
    }
}
