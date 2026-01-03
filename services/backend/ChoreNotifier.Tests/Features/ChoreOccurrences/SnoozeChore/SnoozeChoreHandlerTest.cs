using ChoreNotifier.Features.ChoreOccurrences.SnoozeChore;
using ChoreNotifier.Models;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace ChoreNotifier.Tests.Features.ChoreOccurrences.SnoozeChore;

[TestSubject(typeof(SnoozeChoreHandler))]
public class SnoozeChoreHandlerTest : DatabaseTestBase
{
    private readonly SnoozeChoreHandler _handler;

    public SnoozeChoreHandlerTest(DatabaseFixture dbFixture, ClockFixture clockFixture) : base(dbFixture, clockFixture)
    {
        _handler = new SnoozeChoreHandler(
            dbFixture.CreateDbContext(),
            TestClock);
    }

    [Fact]
    public async Task Handle_WhenChoreOccurenceNonExistent_ReturnsNotFoundError()
    {
        // Arrange
        var request = new SnoozeChoreRequest(UserId: 1, ChoreOccurrenceId: 9999);

        // Act
        var result = await _handler.Handle(request);

        // Assert
        result.IsFailed.Should().BeTrue();
        var error = result
            .Errors
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .BeOfType<NotFoundError>().Which;
        error.EntityName.Should().Be("Chore Occurrence");
        error.EntityKey.Should().Be(9999);
    }

    [Fact]
    public async Task Handle_WhenWrongUser_ReturnsForbiddenError()
    {
        // Arrange
        var choreOccurence = await Factory.CreateChoreOccurrenceAsync();
        var request = new SnoozeChoreRequest(9999, choreOccurence.Id);

        // Act
        var result = await _handler.Handle(request);

        // Assert
        result.Errors
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .BeOfType<ForbiddenError>();
    }

    [Fact]
    public async Task Handle_WhenAlreadyCompleted_ReturnsInvalidOperationError()
    {
        // Arrange
        await using var setupDb = DbFixture.CreateDbContext();
        var choreOccurence = await Factory.CreateChoreOccurrenceAsync(context: setupDb);
        choreOccurence.Complete(choreOccurence.User.Id, TestClock.UtcNow).ThrowIfFailed();
        await setupDb.SaveChangesAsync();

        var request = new SnoozeChoreRequest(choreOccurence.User.Id, choreOccurence.Id);

        // Act
        var result = await _handler.Handle(request);

        // Assert
        result.Errors
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .BeOfType<InvalidOperationError>();
    }

    [Fact]
    public async Task Handle_WhenChoreDoesNotAllowSnooze_ReturnsInvalidOperationError()
    {
        // Arrange
        await using var setupDb = DbFixture.CreateDbContext();
        var chore = await Factory.CreateChoreAsync(context: setupDb, numAssignees: 1);
        chore.UpdateSnoozeDuration(null);
        await setupDb.SaveChangesAsync();

        var choreOccurence = await Factory.CreateChoreOccurrenceAsync(chore: chore, context: setupDb);
        var request = new SnoozeChoreRequest(choreOccurence.User.Id, choreOccurence.Id);

        // Act
        var result = await _handler.Handle(request);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .BeOfType<InvalidOperationError>();
    }

    [Fact]
    public async Task Handle_WhenChoreAllowsSnooze_SnoozesChoreOccurrence()
    {
        // Arrange
        TestClock.FreezeTime();
        var choreOccurence = await Factory.CreateChoreOccurrenceAsync();
        var request = new SnoozeChoreRequest(choreOccurence.User.Id, choreOccurence.Id);
        var originalTime = choreOccurence.DueAt;

        // Act
        var result = await _handler.Handle(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await using var assertDb = DbFixture.CreateDbContext();
        var updatedChoreOccurrence = await assertDb.ChoreOccurrences.FindAsync(choreOccurence.Id);
        updatedChoreOccurrence.Should().NotBeNull();
        updatedChoreOccurrence.DueAt.Should().BeCloseTo(originalTime.Add(choreOccurence.Chore.SnoozeDuration!.Value), TimeSpan.FromMilliseconds(1));
    }
}
