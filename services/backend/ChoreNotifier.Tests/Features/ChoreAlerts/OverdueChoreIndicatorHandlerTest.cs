using ChoreNotifier.Features.ChoreAlerts;
using ChoreNotifier.Infrastructure.ChoreAlerts;
using ChoreNotifier.Models;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ChoreNotifier.Tests.Features.ChoreAlerts;

[TestSubject(typeof(OverdueChoreIndicatorHandler))]
public class OverdueChoreIndicatorHandlerTest : DatabaseTestBase
{
    private readonly Mock<IChoreAlertIndicator> _alertIndicatorMock = new();

    public OverdueChoreIndicatorHandlerTest(DatabaseFixture dbFixture, ClockFixture clockFixture)
        : base(dbFixture, clockFixture)
    {
    }

    private OverdueChoreIndicatorHandler CreateHandler()
    {
        return new OverdueChoreIndicatorHandler(
            DbFixture.CreateDbContext(),
            _alertIndicatorMock.Object,
            TestClock,
            NullLogger<OverdueChoreIndicatorHandler>.Instance);
    }

    [Fact]
    public async Task UpdateAlertStateAsync_WhenNoChoreOccurrences_SetsStateToOk()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        await handler.UpdateAlertStateAsync();

        // Assert
        _alertIndicatorMock.Verify(
            a => a.SetStateAsync(ChoreAlertState.Ok, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAlertStateAsync_WhenHasOverdueChore_SetsStateToOverdue()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        TestClock.SetTime(now);

        await using (var context = DbFixture.CreateDbContext())
        {
            var user = await Factory.CreateUserAsync(context: context);
            var chore = await Factory.CreateChoreAsync(context: context);
            chore.AddAssignee(user);
            await context.SaveChangesAsync();

            // Create occurrence that was due 1 hour ago
            var occurrence = new ChoreOccurrence(chore, user, now.AddHours(-1));
            context.ChoreOccurrences.Add(occurrence);
            await context.SaveChangesAsync();
        }

        var handler = CreateHandler();

        // Act
        await handler.UpdateAlertStateAsync();

        // Assert
        _alertIndicatorMock.Verify(
            a => a.SetStateAsync(ChoreAlertState.Overdue, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAlertStateAsync_WhenChoreIsCompleted_SetsStateToOk()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        TestClock.SetTime(now);

        await using (var context = DbFixture.CreateDbContext())
        {
            var user = await Factory.CreateUserAsync(context: context);
            var chore = await Factory.CreateChoreAsync(context: context);
            chore.AddAssignee(user);
            await context.SaveChangesAsync();

            // Create occurrence that was due 1 hour ago but is completed
            var occurrence = new ChoreOccurrence(chore, user, now.AddHours(-1));
            occurrence.Complete(user.Id, now.AddMinutes(-30));
            context.ChoreOccurrences.Add(occurrence);
            await context.SaveChangesAsync();
        }

        var handler = CreateHandler();

        // Act
        await handler.UpdateAlertStateAsync();

        // Assert
        _alertIndicatorMock.Verify(
            a => a.SetStateAsync(ChoreAlertState.Ok, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAlertStateAsync_WhenChoreIsNotYetDue_SetsStateToOk()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        TestClock.SetTime(now);

        await using (var context = DbFixture.CreateDbContext())
        {
            var user = await Factory.CreateUserAsync(context: context);
            var chore = await Factory.CreateChoreAsync(context: context);
            chore.AddAssignee(user);
            await context.SaveChangesAsync();

            // Create occurrence that is due in the future
            var occurrence = new ChoreOccurrence(chore, user, now.AddHours(1));
            context.ChoreOccurrences.Add(occurrence);
            await context.SaveChangesAsync();
        }

        var handler = CreateHandler();

        // Act
        await handler.UpdateAlertStateAsync();

        // Assert
        _alertIndicatorMock.Verify(
            a => a.SetStateAsync(ChoreAlertState.Ok, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAlertStateAsync_WhenOneOverdueAndOneCompleted_SetsStateToOverdue()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        TestClock.SetTime(now);

        await using (var context = DbFixture.CreateDbContext())
        {
            var user = await Factory.CreateUserAsync(context: context);
            var chore1 = await Factory.CreateChoreAsync(title: "Overdue chore", context: context);
            var chore2 = await Factory.CreateChoreAsync(title: "Completed chore", context: context);
            chore1.AddAssignee(user);
            chore2.AddAssignee(user);
            await context.SaveChangesAsync();

            // One overdue occurrence
            var overdueOccurrence = new ChoreOccurrence(chore1, user, now.AddHours(-1));
            context.ChoreOccurrences.Add(overdueOccurrence);

            // One completed occurrence
            var completedOccurrence = new ChoreOccurrence(chore2, user, now.AddHours(-2));
            completedOccurrence.Complete(user.Id, now.AddHours(-1));
            context.ChoreOccurrences.Add(completedOccurrence);

            await context.SaveChangesAsync();
        }

        var handler = CreateHandler();

        // Act
        await handler.UpdateAlertStateAsync();

        // Assert
        _alertIndicatorMock.Verify(
            a => a.SetStateAsync(ChoreAlertState.Overdue, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAlertStateAsync_WhenChoreBecomesDueExactlyNow_SetsStateToOverdue()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        TestClock.SetTime(now);

        await using (var context = DbFixture.CreateDbContext())
        {
            var user = await Factory.CreateUserAsync(context: context);
            var chore = await Factory.CreateChoreAsync(context: context);
            chore.AddAssignee(user);
            await context.SaveChangesAsync();

            // Create occurrence that is due exactly now
            var occurrence = new ChoreOccurrence(chore, user, now);
            context.ChoreOccurrences.Add(occurrence);
            await context.SaveChangesAsync();
        }

        var handler = CreateHandler();

        // Act
        await handler.UpdateAlertStateAsync();

        // Assert
        _alertIndicatorMock.Verify(
            a => a.SetStateAsync(ChoreAlertState.Overdue, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
