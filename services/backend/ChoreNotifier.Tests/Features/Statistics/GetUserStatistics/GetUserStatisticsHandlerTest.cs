using ChoreNotifier.Features.Statistics.GetUserStatistics;
using ChoreNotifier.Models;
using FluentAssertions;
using JetBrains.Annotations;

namespace ChoreNotifier.Tests.Features.Statistics.GetUserStatistics;

[TestSubject(typeof(GetUserStatisticsHandler))]
public class GetUserStatisticsHandlerTest : DatabaseTestBase
{
    private readonly GetUserStatisticsHandler _handler;

    public GetUserStatisticsHandlerTest(DatabaseFixture dbFixture, ClockFixture clockFixture) : base(dbFixture,
        clockFixture)
    {
        _handler = new GetUserStatisticsHandler(dbFixture.CreateDbContext(), clockFixture.TestClock);
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ReturnsNotFoundError()
    {
        // Arrange
        var request = new GetUserStatisticsRequest(UserId: 9999, StartDate: null, EndDate: null);

        // Act
        var result = await _handler.Handle(request);

        // Assert
        result.IsFailed.Should().BeTrue();
        var error = result.Errors.Should().ContainSingle().Which.Should().BeOfType<NotFoundError>().Subject;
        error.EntityName.Should().Be("User");
        error.EntityKey.Should().Be(9999);
    }

    [Fact]
    public async Task Handle_WhenNoChoreOccurrences_ReturnsZeroStats()
    {
        // Arrange
        var user = await Factory.CreateUserAsync();
        var request = new GetUserStatisticsRequest(UserId: user.Id, StartDate: null, EndDate: null);

        // Act
        var result = await _handler.Handle(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var stats = result.Value;
        stats.TotalChoresAssigned.Should().Be(0);
        stats.TotalChoresCompleted.Should().Be(0);
        stats.SnoozeFrequency.Should().Be(0);
        stats.AverageCompletionTime.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public async Task Handle_WhenChoreOccurrencesExist_ReturnsTotalAssignedAndCompleted()
    {
        // Arrange
        await using var ctx = DbFixture.CreateDbContext();
        var user = await Factory.CreateUserAsync(context: ctx);
        var now = TestClock.UtcNow;
        TestClock.SetTime(now);

        // Create 5 chore occurrences, complete 3 of them
        for (int i = 0; i < 5; i++)
        {
            var occurrence = await Factory.CreateChoreOccurrenceAsync(
                user: user,
                scheduledAt: now.AddHours(-1),
                context: ctx
            );
            if (i < 3)
            {
                occurrence.Complete(user.Id, now);
            }
        }

        await ctx.SaveChangesAsync();

        var request = new GetUserStatisticsRequest(UserId: user.Id, StartDate: null, EndDate: null);

        // Act
        var result = await _handler.Handle(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var stats = result.Value;
        stats.TotalChoresAssigned.Should().Be(5);
        stats.TotalChoresCompleted.Should().Be(3);
    }

    [Fact]
    public async Task Handle_WhenChoresSnoozed_ReturnsCorrectSnoozeFrequency()
    {
        // Arrange
        await using var ctx = DbFixture.CreateDbContext();
        var user = await Factory.CreateUserAsync(context: ctx);
        var now = TestClock.UtcNow;
        TestClock.SetTime(now);

        // Create 4 chore occurrences, snooze 2 of them
        for (int i = 0; i < 4; i++)
        {
            var occurrence = await Factory.CreateChoreOccurrenceAsync(
                user: user,
                scheduledAt: now.AddHours(-1),
                context: ctx
            );
            if (i < 2)
            {
                // Snooze these occurrences (this changes ScheduledFor != DueAt)
                occurrence.Snooze(user.Id, now);
            }
        }

        await ctx.SaveChangesAsync();

        var request = new GetUserStatisticsRequest(UserId: user.Id, StartDate: null, EndDate: null);

        // Act
        var result = await _handler.Handle(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var stats = result.Value;
        stats.SnoozeFrequency.Should().Be(0.5f); // 2 snoozed out of 4 total
    }

    [Fact]
    public async Task Handle_WhenChoresCompleted_ReturnsCorrectAverageCompletionTime()
    {
        // Arrange
        await using var ctx = DbFixture.CreateDbContext();
        var user = await Factory.CreateUserAsync(context: ctx);
        var now = TestClock.UtcNow;
        TestClock.SetTime(now);

        // Create 2 completed chore occurrences with different completion times
        // Occurrence 1: Due 2 hours ago, completed now (2 hours to complete)
        var occurrence1 = await Factory.CreateChoreOccurrenceAsync(
            user: user,
            scheduledAt: now.AddHours(-2),
            context: ctx
        );
        occurrence1.Complete(user.Id, now);

        // Occurrence 2: Due 4 hours ago, completed now (4 hours to complete)
        var occurrence2 = await Factory.CreateChoreOccurrenceAsync(
            user: user,
            scheduledAt: now.AddHours(-4),
            context: ctx
        );
        occurrence2.Complete(user.Id, now);

        await ctx.SaveChangesAsync();

        var request = new GetUserStatisticsRequest(UserId: user.Id, StartDate: null, EndDate: null);

        // Act
        var result = await _handler.Handle(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var stats = result.Value;
        // Average of 2 hours and 4 hours = 3 hours
        stats.AverageCompletionTime.Should().Be(TimeSpan.FromHours(3));
    }

    [Fact]
    public async Task Handle_WhenStartDateProvided_FiltersOccurrencesAfterStartDate()
    {
        // Arrange
        await using var ctx = DbFixture.CreateDbContext();
        var user = await Factory.CreateUserAsync(context: ctx);
        var now = TestClock.UtcNow;
        TestClock.SetTime(now);

        // Create 2 occurrences before the filter date
        for (int i = 0; i < 2; i++)
        {
            await Factory.CreateChoreOccurrenceAsync(
                user: user,
                scheduledAt: now.AddDays(-10),
                context: ctx
            );
        }

        // Create 3 occurrences after the filter date
        for (int i = 0; i < 3; i++)
        {
            await Factory.CreateChoreOccurrenceAsync(
                user: user,
                scheduledAt: now.AddDays(-1),
                context: ctx
            );
        }

        await ctx.SaveChangesAsync();

        var startDate = now.AddDays(-5);
        var request = new GetUserStatisticsRequest(UserId: user.Id, StartDate: startDate, EndDate: null);

        // Act
        var result = await _handler.Handle(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var stats = result.Value;
        stats.TotalChoresAssigned.Should().Be(3);
    }

    [Fact]
    public async Task Handle_WhenEndDateProvided_FiltersOccurrencesBeforeEndDate()
    {
        // Arrange
        await using var ctx = DbFixture.CreateDbContext();
        var user = await Factory.CreateUserAsync(context: ctx);
        var now = TestClock.UtcNow;
        TestClock.SetTime(now);

        // Create 2 occurrences before the filter date
        for (int i = 0; i < 2; i++)
        {
            await Factory.CreateChoreOccurrenceAsync(
                user: user,
                scheduledAt: now.AddDays(-10),
                context: ctx
            );
        }

        // Create 3 occurrences after the filter date
        for (int i = 0; i < 3; i++)
        {
            await Factory.CreateChoreOccurrenceAsync(
                user: user,
                scheduledAt: now.AddDays(5),
                context: ctx
            );
        }

        await ctx.SaveChangesAsync();

        var endDate = now;
        var request = new GetUserStatisticsRequest(UserId: user.Id, StartDate: null, EndDate: endDate);

        // Act
        var result = await _handler.Handle(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var stats = result.Value;
        stats.TotalChoresAssigned.Should().Be(2);
    }

    [Fact]
    public async Task Handle_WhenDateRangeProvided_FiltersOccurrencesWithinRange()
    {
        // Arrange
        await using var ctx = DbFixture.CreateDbContext();
        var user = await Factory.CreateUserAsync(context: ctx);
        var now = TestClock.UtcNow;
        TestClock.SetTime(now);

        // Create 2 occurrences before the range
        for (int i = 0; i < 2; i++)
        {
            await Factory.CreateChoreOccurrenceAsync(
                user: user,
                scheduledAt: now.AddDays(-20),
                context: ctx
            );
        }

        // Create 3 occurrences within the range
        for (int i = 0; i < 3; i++)
        {
            await Factory.CreateChoreOccurrenceAsync(
                user: user,
                scheduledAt: now.AddDays(-5),
                context: ctx
            );
        }

        // Create 4 occurrences after the range
        for (int i = 0; i < 4; i++)
        {
            await Factory.CreateChoreOccurrenceAsync(
                user: user,
                scheduledAt: now.AddDays(10),
                context: ctx
            );
        }

        await ctx.SaveChangesAsync();

        var startDate = now.AddDays(-10);
        var endDate = now;
        var request = new GetUserStatisticsRequest(UserId: user.Id, StartDate: startDate, EndDate: endDate);

        // Act
        var result = await _handler.Handle(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var stats = result.Value;
        stats.TotalChoresAssigned.Should().Be(3);
    }

    [Fact]
    public async Task Handle_WhenOtherUsersHaveOccurrences_OnlyCountsRequestedUser()
    {
        // Arrange
        await using var ctx = DbFixture.CreateDbContext();
        var user1 = await Factory.CreateUserAsync(context: ctx);
        var user2 = await Factory.CreateUserAsync(context: ctx);
        var now = TestClock.UtcNow;
        TestClock.SetTime(now);

        // Create 3 occurrences for user1
        for (int i = 0; i < 3; i++)
        {
            await Factory.CreateChoreOccurrenceAsync(
                user: user1,
                scheduledAt: now,
                context: ctx
            );
        }

        // Create 5 occurrences for user2
        for (int i = 0; i < 5; i++)
        {
            await Factory.CreateChoreOccurrenceAsync(
                user: user2,
                scheduledAt: now,
                context: ctx
            );
        }

        await ctx.SaveChangesAsync();

        var request = new GetUserStatisticsRequest(UserId: user1.Id, StartDate: null, EndDate: null);

        // Act
        var result = await _handler.Handle(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var stats = result.Value;
        stats.TotalChoresAssigned.Should().Be(3);
    }
}
