using ChoreNotifier.Features.Chores.CreateChore;
using ChoreNotifier.Features.Chores.Scheduling;
using ChoreNotifier.Models;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace ChoreNotifier.Tests.Features.Chores.CreateChore;

[TestSubject(typeof(CreateChoreHandler))]
public class CreateChoreHandlerTest : DatabaseTestBase
{
    private readonly CreateChoreHandler _handler;

    public CreateChoreHandlerTest(DatabaseFixture dbFixture) : base(dbFixture)
    {
        _handler = new CreateChoreHandler(
            dbFixture.CreateDbContext(),
            new ChoreSchedulingService(),
            TestClock
        );
    }

    private static CreateChoreRequest CreateValidRequest() => new(
        "Test Chore",
        "This is a test chore",
        new CreateChoreScheduleRequest
        (
            DateTime.UtcNow,
            7,
            null
        ),
        TimeSpan.FromDays(2),
        new List<int>()
    );

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(5)]
    public async Task Handle_WhenValid_CreatesChore(int assigneeCount)
    {
        // Arrange
        var users = await Factory.CreateUsersAsync(assigneeCount);
        var originalUserIds = users.Select(u => u.Id).ToList();

        var request = CreateValidRequest() with { AssigneeUserIds = originalUserIds };

        // Act
        var result = await _handler.Handle(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().NotBe(0);
        result.Value.Title.Should().Be("Test Chore");

        using var context = DbFixture.CreateDbContext();
        var choreInDb = await context.Chores
            .Include(c => c.Assignees)
            .ThenInclude(a => a.User)
            .FirstOrDefaultAsync(u => u.Id == result.Value.Id);
        choreInDb.Should().NotBeNull();
        choreInDb!.Title.Should().Be("Test Chore");
        choreInDb.Description.Should().Be("This is a test chore");
        choreInDb.ChoreSchedule.IntervalDays.Should().Be(7);
        var assigneeUserIds = choreInDb.Assignees.Select(a => a.User.Id).OrderBy(id => id);
        assigneeUserIds.Should().BeEquivalentTo(originalUserIds.OrderBy(id => id));
    }

    [Fact]
    public async Task Handle_WhenTitleIsEmpty_ReturnsValidationError()
    {
        // Arrange
        var request = CreateValidRequest() with { Title = "" };

        // Act
        var result = await _handler.Handle(request);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .BeOfType<ValidationError>()
            .Which
            .Message.Should().Contain("Title cannot be empty");
    }

    [Fact]
    public async Task Handle_WhenNonExistentAssignee_ReturnsNotFoundError()
    {
        // Arrange
        var request = CreateValidRequest() with { AssigneeUserIds = new List<int> { 9999 } };

        // Act
        var result = await _handler.Handle(request);

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
                e.EntityName == "Users" &&
                (string)e.EntityKey == "9999");
    }

    [Fact]
    public async Task Handle_WhenHasAssignees_SetsUpNextOccurrence()
    {
        // Arrange
        var users = await Factory.CreateUsersAsync(2);
        var request = CreateValidRequest() with { AssigneeUserIds = users.Select(u => u.Id).ToList() };

        // Act
        var result = await _handler.Handle(request);

        // Assert
        result.IsSuccess.Should().BeTrue();

        using var context = DbFixture.CreateDbContext();
        var occuruence = await context.ChoreOccurrences
            .FirstOrDefaultAsync(o => o.ChoreId == result.Value.Id);
        occuruence.Should().NotBeNull();
        occuruence.ScheduledFor.Should().BeAfter(TestClock.UtcNow);
    }

    [Fact]
    public async Task Handle_WhenNoAssignees_DoesNotSetUpNextOccurrence()
    {
        // Arrange
        var request = CreateValidRequest() with { AssigneeUserIds = new List<int>() };

        // Act
        var result = await _handler.Handle(request);

        // Assert
        result.IsSuccess.Should().BeTrue();

        using var context = DbFixture.CreateDbContext();
        var occuruence = await context.ChoreOccurrences
            .FirstOrDefaultAsync(o => o.ChoreId == result.Value.Id);
        occuruence.Should().BeNull();
    }
}
