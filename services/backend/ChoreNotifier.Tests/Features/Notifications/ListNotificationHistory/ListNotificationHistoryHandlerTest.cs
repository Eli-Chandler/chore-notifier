using ChoreNotifier.Features.Notifications.ListNotificationHistory;
using ChoreNotifier.Models;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace ChoreNotifier.Tests.Features.Notifications.ListNotificationHistory;

[TestSubject(typeof(ListNotificationHistoryHandler))]
public class ListNotificationHistoryHandlerTest : DatabaseTestBase
{
    private readonly ListNotificationHistoryHandler _handler;

    public ListNotificationHistoryHandlerTest(DatabaseFixture dbFixture, ClockFixture clockFixture)
        : base(dbFixture, clockFixture)
    {
        _handler = new ListNotificationHistoryHandler(dbFixture.CreateDbContext());
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ReturnsNotFoundError()
    {
        // Arrange
        var nonExistentUserId = 999;

        // Act
        var result = await _handler.Handle(new ListNotificationHistoryRequest(nonExistentUserId, 10));

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors
            .Should()
            .ContainSingle()
            .Which
            .Message.Should().Contain("User")
            .And.Contain(nonExistentUserId.ToString())
            .And.Contain("was not found");
    }

    [Fact]
    public async Task Handle_WhenNoNotifications_ReturnsEmptyList()
    {
        // Arrange
        var user = await Factory.CreateUserAsync();

        // Act
        var result = await _handler.Handle(new ListNotificationHistoryRequest(user.Id, 10));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.HasNextPage.Should().BeFalse();
        result.Value.NextCursor.Should().NotHaveValue();
    }

    [Fact]
    public async Task Handle_WhenNotificationsExist_ReturnsAllNotifications()
    {
        // Arrange
        var user = await Factory.CreateUserAsync();
        var attempts = await CreateNotificationAttemptsAsync(user, 3);

        // Act
        var result = await _handler.Handle(new ListNotificationHistoryRequest(user.Id, 10));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        result.Value.HasNextPage.Should().BeFalse();

        foreach (var attempt in attempts)
        {
            result.Value.Items.Should().Contain(i =>
                i.Id == attempt.Id &&
                i.Title == attempt.Notification.Title &&
                i.Message == attempt.Notification.Message);
        }
    }

    [Fact]
    public async Task Handle_WhenPageSizeSmallerThanTotal_ReturnsPaginatedResults()
    {
        // Arrange
        var user = await Factory.CreateUserAsync();
        await CreateNotificationAttemptsAsync(user, 10);

        // Act
        var result = await _handler.Handle(new ListNotificationHistoryRequest(user.Id, 5));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
        result.Value.HasNextPage.Should().BeTrue();
        result.Value.NextCursor.Should().HaveValue();
    }

    [Fact]
    public async Task Handle_WhenUsingCursor_ReturnsRemainingNotifications()
    {
        // Arrange
        var user = await Factory.CreateUserAsync();
        var attempts = await CreateNotificationAttemptsAsync(user, 10);

        // Act - Get first page (most recent 5)
        var firstPage = await _handler.Handle(new ListNotificationHistoryRequest(user.Id, 5));

        // Act - Get second page using cursor (older 5)
        var secondPage = await _handler.Handle(
            new ListNotificationHistoryRequest(user.Id, 5, firstPage.Value.NextCursor));

        // Assert
        firstPage.Value.Items.Should().HaveCount(5);
        secondPage.Value.Items.Should().HaveCount(5);
        secondPage.Value.HasNextPage.Should().BeFalse();
        secondPage.Value.NextCursor.Should().NotHaveValue();

        // Verify all notifications are returned across both pages
        var allReturnedIds = firstPage.Value.Items.Select(i => i.Id)
            .Concat(secondPage.Value.Items.Select(i => i.Id))
            .ToList();

        var expectedIds = attempts.Select(a => a.Id).ToList();
        allReturnedIds.Should().BeEquivalentTo(expectedIds);

        // Verify ordering: first page should have most recent (higher index = older)
        firstPage.Value.Items.Should().BeInDescendingOrder(i => i.AttemptedAt);
        secondPage.Value.Items.Should().BeInDescendingOrder(i => i.AttemptedAt);

        // First page items should be more recent than second page items
        firstPage.Value.Items.Min(i => i.AttemptedAt)
            .Should().BeAfter(secondPage.Value.Items.Max(i => i.AttemptedAt));
    }

    [Fact]
    public async Task Handle_WhenPageSizeIsZero_ReturnsError()
    {
        // Arrange
        var user = await Factory.CreateUserAsync();

        // Act
        var result = await _handler.Handle(new ListNotificationHistoryRequest(user.Id, 0));

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors
            .Should()
            .ContainSingle()
            .Which
            .Message.Should().Contain("Page size must be greater than 0");
    }

    [Fact]
    public async Task Handle_WhenPageSizeIsNegative_ReturnsError()
    {
        // Arrange
        var user = await Factory.CreateUserAsync();

        // Act
        var result = await _handler.Handle(new ListNotificationHistoryRequest(user.Id, -1));

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors
            .Should()
            .ContainSingle()
            .Which
            .Message.Should().Contain("Page size must be greater than 0");
    }

    [Fact]
    public async Task Handle_WhenPageSizeExceeds100_ReturnsError()
    {
        // Arrange
        var user = await Factory.CreateUserAsync();

        // Act
        var result = await _handler.Handle(new ListNotificationHistoryRequest(user.Id, 101));

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors
            .Should()
            .ContainSingle()
            .Which
            .Message.Should().Contain("Page size cannot exceed 100");
    }

    [Fact]
    public async Task Handle_ReturnsCorrectNotificationDetails()
    {
        // Arrange
        var user = await Factory.CreateUserAsync();
        var now = DateTimeOffset.UtcNow;
        TestClock.SetTime(now);

        await using (var context = DbFixture.CreateDbContext())
        {
            var userFromDb = await context.Users.FindAsync(user.Id);

            var attempt = new NotificationAttempt
            {
                Notification = new Notification("Test Title", "Test Message"),
                NotificationType = NotificationType.Console,
                AttemptedAt = now,
                Recipient = userFromDb!
            };
            attempt.MarkDelivered(now.AddSeconds(1));

            context.NotificationAttempts.Add(attempt);
            await context.SaveChangesAsync();
        }

        // Act
        var result = await _handler.Handle(new ListNotificationHistoryRequest(user.Id, 10));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle();

        var item = result.Value.Items.First();
        item.Title.Should().Be("Test Title");
        item.Message.Should().Be("Test Message");
        item.NotificationType.Should().Be(NotificationType.Console);
        item.AttemptedAt.Should().Be(now);
        item.DeliveryStatus.Should().Be(DeliveryStatus.Delivered);
        item.DeliveredAt.Should().Be(now.AddSeconds(1));
        item.FailureReason.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenNotificationFailed_ReturnsFailureDetails()
    {
        // Arrange
        var user = await Factory.CreateUserAsync();
        var now = DateTimeOffset.UtcNow;
        TestClock.SetTime(now);

        await using (var context = DbFixture.CreateDbContext())
        {
            var userFromDb = await context.Users.FindAsync(user.Id);

            var attempt = new NotificationAttempt
            {
                Notification = new Notification("Failed Title", "Failed Message"),
                NotificationType = NotificationType.Ntfy,
                AttemptedAt = now,
                Recipient = userFromDb!
            };
            attempt.MarkFailed("Connection timeout");

            context.NotificationAttempts.Add(attempt);
            await context.SaveChangesAsync();
        }

        // Act
        var result = await _handler.Handle(new ListNotificationHistoryRequest(user.Id, 10));

        // Assert
        result.IsSuccess.Should().BeTrue();
        var item = result.Value.Items.First();
        item.DeliveryStatus.Should().Be(DeliveryStatus.Failed);
        item.FailureReason.Should().Be("Connection timeout");
        item.DeliveredAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_OnlyReturnsNotificationsForRequestedUser()
    {
        // Arrange
        var user1 = await Factory.CreateUserAsync("User 1");
        var user2 = await Factory.CreateUserAsync("User 2");

        await CreateNotificationAttemptsAsync(user1, 3);
        await CreateNotificationAttemptsAsync(user2, 5);

        // Act
        var result = await _handler.Handle(new ListNotificationHistoryRequest(user1.Id, 10));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_ReturnsNotificationsInDescendingDateOrder()
    {
        // Arrange
        var user = await Factory.CreateUserAsync();
        await CreateNotificationAttemptsAsync(user, 5);

        // Act
        var result = await _handler.Handle(new ListNotificationHistoryRequest(user.Id, 10));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeInDescendingOrder(i => i.AttemptedAt);
    }

    private async Task<List<NotificationAttempt>> CreateNotificationAttemptsAsync(User user, int count)
    {
        var attempts = new List<NotificationAttempt>();
        var baseTime = DateTimeOffset.UtcNow;

        await using var context = DbFixture.CreateDbContext();
        var userFromDb = await context.Users.FindAsync(user.Id);

        for (int i = 0; i < count; i++)
        {
            var attempt = new NotificationAttempt
            {
                Notification = new Notification($"Title {i}", $"Message {i}"),
                NotificationType = NotificationType.Console,
                AttemptedAt = baseTime.AddMinutes(-i),
                Recipient = userFromDb!
            };
            attempt.MarkDelivered(baseTime.AddMinutes(-i).AddSeconds(1));

            context.NotificationAttempts.Add(attempt);
            attempts.Add(attempt);
        }

        await context.SaveChangesAsync();
        return attempts;
    }
}
