using ChoreNotifier.Features.Chores.RemoveChoreAssignee;
using ChoreNotifier.Models;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace ChoreNotifier.Tests.Features.Chores.RemoveChoreAssignee;

[TestSubject(typeof(RemoveChoreAssigneeHandler))]
public class RemoveChoreAssigneeHandlerTest : DatabaseTestBase
{
    private readonly RemoveChoreAssigneeHandler _handler;

    public RemoveChoreAssigneeHandlerTest(DatabaseFixture dbFixture, ClockFixture clockFixture) : base(dbFixture, clockFixture)
    {
        _handler = new RemoveChoreAssigneeHandler(dbFixture.CreateDbContext());
    }

    [Fact]
    public async Task Handle_WhenChoreNonExistent_ThrowsNotFoundException()
    {
        // Arrange
        var user = await Factory.CreateUserAsync();

        // Act
        var result = await _handler.Handle(new RemoveChoreAssigneeRequest(9999, user.Id));

        // Assert
        var error = result.Errors
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .BeOfType<NotFoundError>()
            .Subject;

        error.EntityName.Should().Be("Chore");
        error.EntityKey.Should().Be("9999");
    }

    [Fact]
    public async Task Handle_WhenUserNonExistent_ThrowsNotFoundException()
    {
        // Arrange
        var chore = await Factory.CreateChoreAsync();

        // Act
        var result = await _handler.Handle(new RemoveChoreAssigneeRequest(chore.Id, 9999));

        // Assert
        var error = result.Errors
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .BeOfType<NotFoundError>()
            .Subject;

        error.EntityName.Should().Be("User");
        error.EntityKey.Should().Be("9999");
    }

    [Fact]
    public async Task Handle_WhenUserAndChoreNonExistent_ThrowsNotFoundException()
    {
        // Act
        var result = await _handler.Handle(new RemoveChoreAssigneeRequest(9999, 9999));

        // Assert
        var error = result.Errors
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .BeOfType<NotFoundError>()
            .Subject;

        error.EntityName.Should().Be("Chore");
        error.EntityKey.Should().Be("9999");
    }

    [Fact]
    public async Task Handle_WhenUserNotAnAssignee_ThrowsConflictException()
    {
        // Arrange
        var chore = await Factory.CreateChoreAsync(numAssignees: 1);
        var user = await Factory.CreateUserAsync();

        // Act
        var result = await _handler.Handle(new RemoveChoreAssigneeRequest(chore.Id, user.Id));

        // Assert
        var error = result.Errors
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .BeOfType<InvalidOperationError>()
            .Subject;

        error.Message.Should().Be("User is not assigned to this chore.");
    }

    [Fact]
    public async Task Handle_WhenValidRequest_RemovesAssignee()
    {
        // Arrange
        var chore = await Factory.CreateChoreAsync(numAssignees: 1);
        var user = chore.Assignees.First().User;

        // Act
        var result = await _handler.Handle(new RemoveChoreAssigneeRequest(chore.Id, user.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        await using var assertDb = DbFixture.CreateDbContext();
        var updatedChore = await assertDb.Chores
            .Include(c => c.Assignees)
            .FirstAsync();

        updatedChore.Assignees.Should().BeEmpty();
    }
}
