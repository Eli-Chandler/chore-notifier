using ChoreNotifier.Features.ChoreOccurrences.CompleteChore;
using ChoreNotifier.Features.Chores.Scheduling;
using ChoreNotifier.Models;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace ChoreNotifier.Tests.Features.ChoreOccurences.CompleteChore;

[TestSubject(typeof(CompleteChoreHandler))]
public class CompleteChoreHandlerTest : DatabaseTestBase
{
    private readonly CompleteChoreHandler _handler;

    public CompleteChoreHandlerTest(DatabaseFixture dbFixture, ClockFixture clockFixture) : base(dbFixture, clockFixture)
    {
        _handler = new CompleteChoreHandler(
            dbFixture.CreateDbContext(),
            new ChoreSchedulingService(),
            TestClock);
    }

    [Fact]
    public async Task Handle_WhenChoreOccurenceNonExistent_ReturnsNotFoundError()
    {
        // Arrange
        var request = new CompleteChoreRequest(UserId: 1, ChoreOccurrenceId: 9999);

        // Act
        var result = await _handler.Handle(request);

        // Assert
        result.Errors
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .Match<NotFoundError>(e =>
                e.EntityName == "Chore Occurrence" &&
                (int)e.EntityKey == 9999);
    }

    [Fact]
    public async Task Handle_WhenWrongUser_ReturnsForbiddenError()
    {
        // Arrange
        var choreOccurence = await Factory.CreateChoreOccurrenceAsync();
        var request = new CompleteChoreRequest(9999, choreOccurence.Id);

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

        var request = new CompleteChoreRequest(choreOccurence.User.Id, choreOccurence.Id);

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
    public async Task Handle_WhenValidRequest_CompletesChoreOccurrence()
    {
        // Arrange
        TestClock.FreezeTime();
        var choreOccurence = await Factory.CreateChoreOccurrenceAsync();
        var request = new CompleteChoreRequest(choreOccurence.User.Id, choreOccurence.Id);

        // Act
        var result = await _handler.Handle(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await using var assertDb = DbFixture.CreateDbContext();
        var updatedChoreOccurrence = await assertDb.ChoreOccurrences.FindAsync(choreOccurence.Id);
        updatedChoreOccurrence.Should().NotBeNull();
        updatedChoreOccurrence!.CompletedAt.Should().Be(TestClock.UtcNow);

        var nextOccurrence = await assertDb.ChoreOccurrences
            .FirstOrDefaultAsync(co => co.ChoreId == choreOccurence.ChoreId && co.Id != choreOccurence.Id);
        nextOccurrence.Should().NotBeNull();
        nextOccurrence!.ScheduledFor.Should().BeCloseTo(TestClock.UtcNow.AddDays(choreOccurence.Chore.ChoreSchedule.IntervalDays), TimeSpan.FromMilliseconds(1));
    }
}
