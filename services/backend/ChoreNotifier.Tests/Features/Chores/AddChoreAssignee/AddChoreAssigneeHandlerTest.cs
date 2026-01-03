using ChoreNotifier.Features.Chores.AddChoreAssignee;
using ChoreNotifier.Features.Chores.Scheduling;
using ChoreNotifier.Models;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace ChoreNotifier.Tests.Features.Chores.AddChoreAssignee;

[TestSubject(typeof(AddChoreAssigneeHandler))]
public class AddChoreAssigneeHandlerTest : DatabaseTestBase
{
    private readonly AddChoreAssigneeHandler _handler;

    public AddChoreAssigneeHandlerTest(DatabaseFixture dbFixture, ClockFixture clockFixture) : base(dbFixture, clockFixture)
    {
        _handler = new AddChoreAssigneeHandler(dbFixture.CreateDbContext(), new ChoreSchedulingService(), TestClock);
    }

    [Fact]
    public async Task Handle_WhenChoreNonExistent_ThrowsNotFoundException()
    {
        // Arrange
        var user = await Factory.CreateUserAsync();
        var request = new AddChoreAssigneeRequest(user.Id);

        // Act
        var result = await _handler.Handle(9999, request);

        // Assert
        result.Errors
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .BeOfType<NotFoundError>()
            .Which
            .Should()
            .Match<NotFoundError>(e =>
                e.EntityName == "Chore" &&
                (string)e.EntityKey == "9999");
    }

    [Fact]
    public async Task Handle_WhenUserNonExistent_ThrowsNotFoundException()
    {
        // Arrange
        var chore = await Factory.CreateChoreAsync();
        var request = new AddChoreAssigneeRequest(9999);

        // Act
        var result = await _handler.Handle(chore.Id, request);

        // Assert
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
                (string)e.EntityKey == "9999");
    }

    [Fact]
    public async Task Handle_WhenUserAlreadyAssignee_ThrowsConflictException()
    {
        // Arrange
        var chore = await Factory.CreateChoreAsync(numAssignees: 1);
        var user = chore.Assignees.First().User;
        var request = new AddChoreAssigneeRequest(user.Id);

        // Act
        var result = await _handler.Handle(chore.Id, request);

        // Assert
        result.Errors
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .BeOfType<ConflictError>()
            .Which
            .Should()
            .Match<ConflictError>(e =>
                e.Message.Contains("User is already an assignee."));
    }

    [Fact]
    public async Task Handle_WhenValidRequest_AddsAssigneeAndReturnsResponse()
    {
        // Arrange
        var chore = await Factory.CreateChoreAsync();
        var user = await Factory.CreateUserAsync();
        var request = new AddChoreAssigneeRequest(user.Id);

        // Act
        var result = await _handler.Handle(chore.Id, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Assignees.Should().ContainSingle().Which.Id.Should().Be(user.Id);

        await using var context = DbFixture.CreateDbContext();
        var choreInDb = await context.Chores
            .Include(c => c.Assignees)
            .ThenInclude(a => a.User)
            .FirstOrDefaultAsync(c => c.Id == chore.Id);
        choreInDb.Should().NotBeNull();
        choreInDb.Assignees.Should().ContainSingle().Which.User.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task Handle_WhenNoExistingAssigneesAndNoExistingOccurrences_SetsNextOccurrence()
    {
        // Arrange
        var chore = await Factory.CreateChoreAsync();
        var user = await Factory.CreateUserAsync();
        var request = new AddChoreAssigneeRequest(user.Id);

        // Act
        var result = await _handler.Handle(chore.Id, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await using var context = DbFixture.CreateDbContext();
        var occurrenceInDb = await context.ChoreOccurrences
            .FirstOrDefaultAsync(o => o.ChoreId == chore.Id);
        occurrenceInDb.Should().NotBeNull();
    }
}
