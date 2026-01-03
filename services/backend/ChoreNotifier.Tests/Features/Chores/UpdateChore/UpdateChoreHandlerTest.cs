using ChoreNotifier.Features.Chores.CreateChore;
using ChoreNotifier.Features.Chores.UpdateChore;
using ChoreNotifier.Models;
using FluentAssertions;
using JetBrains.Annotations;

namespace ChoreNotifier.Tests.Features.Chores.UpdateChore;

[TestSubject(typeof(UpdateChoreHandler))]
public class UpdateChoreHandlerTest : DatabaseTestBase
{
    private readonly UpdateChoreHandler _handler;

    public UpdateChoreHandlerTest(DatabaseFixture dbFixture) : base(dbFixture)
    {
        _handler = new UpdateChoreHandler(dbFixture.CreateDbContext());
    }

    public static UpdateChoreRequest CreateValidRequest() => new(
        "Updated Chore",
        "This is an updated chore description",
        new CreateChoreScheduleRequest
        (
            DateTime.UtcNow.AddDays(1),
            5,
            null
        ),
        TimeSpan.FromDays(3)
    );

    [Fact]
    public async Task Handle_WhenChoreNonExistent_ReturnsNotFoundError()
    {
        // Arrange
        var req = CreateValidRequest();

        // Act
        var result = await _handler.Handle(999, req);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().BeOfType<NotFoundError>();
    }

    [Fact]
    public async Task Handle_WhenRequestIsInvalid_ReturnsValidationErrors()
    {
        // Arrange
        var chore = await Factory.CreateChoreAsync();
        var req = CreateValidRequest() with
        {
            Title = string.Empty,
            Description = "A".PadLeft(1001, 'A'),
            SnoozeDuration = TimeSpan.Zero
        };

        // Act
        var result = await _handler.Handle(chore.Id, req);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(3);
        result.Errors.Should().AllBeOfType<ValidationError>();
    }

    [Fact]
    public async Task Handle_WhenRequestIsValid_UpdatesChoreSuccessfully()
    {
        // Arrange
        var chore = await Factory.CreateChoreAsync();
        var req = CreateValidRequest();

        // Act
        var result = await _handler.Handle(chore.Id, req);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var updatedChore = result.Value;
        updatedChore.Id.Should().Be(chore.Id);
        updatedChore.Title.Should().Be(req.Title);
        updatedChore.Description.Should().Be(req.Description);
        updatedChore.SnoozeDuration.Should().Be(req.SnoozeDuration);
        updatedChore.ChoreSchedule.Start.Should().BeCloseTo(req.ChoreSchedule!.Start, TimeSpan.FromSeconds(1));
        updatedChore.ChoreSchedule.IntervalDays.Should().Be(req.ChoreSchedule.IntervalDays);
        updatedChore.ChoreSchedule.Until.Should().Be(req.ChoreSchedule.Until);
    }

    [Fact]
    public async Task Handle_WhenChoreScheduleIsNull_DoesNotUpdateChoreSchedule()
    {
        // Arrange
        var chore = await Factory.CreateChoreAsync();
        var originalSchedule = chore.ChoreSchedule;
        var req = CreateValidRequest() with { ChoreSchedule = null };

        // Act
        var result = await _handler.Handle(chore.Id, req);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var updatedChore = result.Value;
        updatedChore.Id.Should().Be(chore.Id);
        updatedChore.Title.Should().Be(req.Title);
        updatedChore.Description.Should().Be(req.Description);
        updatedChore.SnoozeDuration.Should().Be(req.SnoozeDuration);
        updatedChore.ChoreSchedule.Start.Should().Be(originalSchedule.Start);
        updatedChore.ChoreSchedule.IntervalDays.Should().Be(originalSchedule.IntervalDays);
        updatedChore.ChoreSchedule.Until.Should().Be(originalSchedule.Until);
    }
}