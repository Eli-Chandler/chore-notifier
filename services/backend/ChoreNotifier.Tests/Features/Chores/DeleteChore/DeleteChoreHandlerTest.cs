using ChoreNotifier.Features.Chores.DeleteChore;
using ChoreNotifier.Models;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace ChoreNotifier.Tests.Features.Chores.DeleteChore;

[TestSubject(typeof(DeleteChoreHandler))]
public class DeleteChoreHandlerTest : DatabaseTestBase
{
    private readonly DeleteChoreHandler _handler;

    public DeleteChoreHandlerTest(DatabaseFixture dbFixture, ClockFixture clockFixture) : base(dbFixture, clockFixture)
    {
        _handler = new DeleteChoreHandler(dbFixture.CreateDbContext());
    }

    [Fact]
    public async Task Handle_WhenChoreExists_DeletesChore()
    {
        // Arrange
        var chore = await Factory.CreateChoreAsync();

        // Act
        var result = await _handler.Handle(new DeleteChoreRequest(chore.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();

        await using var context = DbFixture.CreateDbContext();
        var choreInDb = await context.Chores.FindAsync(chore.Id);
        choreInDb.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenChoreDoesNotExist_ReturnsNotFoundError()
    {
        // Arrange & Act
        var result = await _handler.Handle(new DeleteChoreRequest(9999));

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
                e.EntityName == "Chore" &&
                (int)e.EntityKey == 9999);
    }

    [Fact]
    public async Task Handle_WhenMultipleChoresExist_DeletesOnlySpecifiedChore()
    {
        // Arrange
        var chore1 = await Factory.CreateChoreAsync(title: "Chore 1");
        var chore2 = await Factory.CreateChoreAsync(title: "Chore 2");
        var chore3 = await Factory.CreateChoreAsync(title: "Chore 3");

        // Act
        var result = await _handler.Handle(new DeleteChoreRequest(chore2.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();

        await using var context = DbFixture.CreateDbContext();
        var deletedChore = await context.Chores.FindAsync(chore2.Id);
        deletedChore.Should().BeNull();

        var remainingChore1 = await context.Chores.FindAsync(chore1.Id);
        remainingChore1.Should().NotBeNull();

        var remainingChore3 = await context.Chores.FindAsync(chore3.Id);
        remainingChore3.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WhenChoreHasAssignees_DeletesChoreAndAssignees()
    {
        // Arrange
        var chore = await Factory.CreateChoreAsync(numAssignees: 2);
        var assigneeIds = chore.Assignees.Select(a => a.Id).ToList();

        // Act
        var result = await _handler.Handle(new DeleteChoreRequest(chore.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();

        await using var context = DbFixture.CreateDbContext();
        var choreInDb = await context.Chores.FindAsync(chore.Id);
        choreInDb.Should().BeNull();

        var assigneesInDb = await context.Set<ChoreAssignee>()
            .Where(a => assigneeIds.Contains(a.Id))
            .ToListAsync();
        assigneesInDb.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenChoreHasOccurrences_DeletesChoreAndOccurrences()
    {
        // Arrange
        var occurrence = await Factory.CreateChoreOccurrenceAsync();
        var choreId = occurrence.ChoreId;
        var occurrenceId = occurrence.Id;

        // Act
        var result = await _handler.Handle(new DeleteChoreRequest(choreId));

        // Assert
        result.IsSuccess.Should().BeTrue();

        await using var context = DbFixture.CreateDbContext();
        var choreInDb = await context.Chores.FindAsync(choreId);
        choreInDb.Should().BeNull();

        var occurrenceInDb = await context.ChoreOccurrences.FindAsync(occurrenceId);
        occurrenceInDb.Should().BeNull();
    }
}
