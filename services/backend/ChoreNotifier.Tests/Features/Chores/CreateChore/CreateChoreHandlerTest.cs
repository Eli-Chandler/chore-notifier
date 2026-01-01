using ChoreNotifier.Common;
using ChoreNotifier.Features.Chores.CreateChore;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace ChoreNotifier.Tests.Features.Chores.CreateChore;

[TestSubject(typeof(CreateChoreHandler))]
public class CreateChoreHandlerTest: DatabaseTestBase, IClassFixture<DatabaseFixture>
{
    private readonly CreateChoreHandler _handler;

    public CreateChoreHandlerTest(DatabaseFixture dbFixture) : base(dbFixture)
    {
        _handler = new CreateChoreHandler(dbFixture.CreateDbContext(), new CreateChoreRequestValidator());
    }

    private static CreateChoreRequest CreateValidRequest() => new()
    {
        Title = "Test Chore",
        Description = "This is a test chore",
        ChoreSchedule = new CreateChoreScheduleRequest
        {
            Start = DateTime.UtcNow,
            IntervalDays = 7
        },
        AllowSnooze = true,
        SnoozeDuration = TimeSpan.FromDays(2),
        AssigneeUserIds = new List<int>()
    };
    
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
        var response = await _handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotEqual(0, response.Id);
        Assert.Equal("Test Chore", response.Title);
        using var context = DbFixture.CreateDbContext();
        var choreInDb = await context.Chores
            .Include(c => c.Assignees)
            .ThenInclude(a => a.User)
            .FirstOrDefaultAsync(u => u.Id == response.Id);
        Assert.NotNull(choreInDb);
        Assert.Equal("Test Chore", choreInDb.Title);
        Assert.Equal("This is a test chore", choreInDb.Description);
        Assert.Equal(7, choreInDb.ChoreSchedule.IntervalDays);
        var assigneeUserIds = choreInDb.Assignees.Select(a => a.User.Id);
        Assert.Equal(assigneeUserIds.OrderBy(id => id), originalUserIds.OrderBy(id => id));
    }

    [Fact]
    public async Task Handle_WhenTitleIsEmpty_ThrowsValidationException()
    {
        // Arrange
        var request = CreateValidRequest() with { Title = "" };

        // Act & Assert
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(async () =>
        {
            await _handler.Handle(request, CancellationToken.None);
        });
    }
    
    [Fact]
    public async Task Handle_WhenNonExistentAssignee_ThrowsNotFoundException()
    {
        // Arrange
        var request = CreateValidRequest() with { AssigneeUserIds = new List<int> { 9999 } };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(async () =>
        {
            await _handler.Handle(request, CancellationToken.None);
        });
    }
    
    
}
