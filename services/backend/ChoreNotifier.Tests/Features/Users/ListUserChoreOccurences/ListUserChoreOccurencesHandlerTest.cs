using ChoreNotifier.Features.Users.ListUserChoreOccurences;
using ChoreNotifier.Models;
using FluentAssertions;
using JetBrains.Annotations;

namespace ChoreNotifier.Tests.Features.Users.ListUserChoreOccurences;

[TestSubject(typeof(ListUserChoreOccurrencesHandler))]
public class ListUserChoreOccurrencesHandlerTest : DatabaseTestBase
{
    private readonly ListUserChoreOccurrencesHandler _handler;

    public ListUserChoreOccurrencesHandlerTest(DatabaseFixture dbFixture, ClockFixture clockFixture) : base(dbFixture,
        clockFixture)
    {
        _handler = new ListUserChoreOccurrencesHandler(dbFixture.CreateDbContext(), clockFixture.TestClock);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(101)]
    public async Task Handle_WhenPageSizeInvalid_ReturnsValidationError(int pageSize)
    {
        // Arrange
        var request = new ListUserChoreOccurrencesRequest(UserId: 1, PageSize: pageSize, AfterId: null);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        var error = result.Errors.Should().ContainSingle().Which.Should().BeOfType<ValidationError>().Subject;
        error.Message.Should().Contain("Page size");
    }

    [Fact]
    public async Task Handle_WhenFilterInvalid_ReturnsValidationError()
    {
        // Arrange
        var invalidFilter = (ChoreOccurenceFilter)999;
        var request =
            new ListUserChoreOccurrencesRequest(UserId: 1, PageSize: 10, AfterId: null, Filter: invalidFilter);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        var error = result.Errors.Should().ContainSingle().Which.Should().BeOfType<ValidationError>().Subject;
        error.Message.Should().Contain("filter");
    }

    [Fact]
    public async Task Handle_WhenUserNonExistent_ReturnsNotFoundError()
    {
        // Arrange
        var request = new ListUserChoreOccurrencesRequest(UserId: 9999, PageSize: 10, AfterId: null);

        // Act
        var result = await _handler.Handle(request);

        // Assert
        result.IsFailed.Should().BeTrue();
        var error = result.Errors.Should().ContainSingle().Which.Should().BeOfType<NotFoundError>().Subject;
        error.EntityName.Should().Be("User");
        error.EntityKey.Should().Be(9999);
    }

    [Fact]
    public async Task Handle_WhenNoChoreOccurrences_ReturnsEmptyPage()
    {
        // Arrange
        var user = await Factory.CreateUserAsync();
        var request = new ListUserChoreOccurrencesRequest(UserId: user.Id, PageSize: 10, AfterId: null);

        // Act
        var result = await _handler.Handle(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var page = result.Value;
        page.Items.Should().BeEmpty();
        page.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenChoreOccurrencesExist_ReturnsChoreOccurrencesPage()
    {
        await using var ctx = DbFixture.CreateDbContext();
        var user = await Factory.CreateUserAsync(context: ctx);

        var choreOccurrences = new List<ChoreOccurrence>();

        for (int i = 0; i < 10; i++)
        {
            choreOccurrences.Add(await Factory.CreateChoreOccurrenceAsync(user: user, context: ctx));
        }

        var request1 = new ListUserChoreOccurrencesRequest(UserId: user.Id, PageSize: 5, AfterId: null);
        var result1 = await _handler.Handle(request1);

        result1.IsSuccess.Should().BeTrue();
        var page = result1.Value;
        page.Items.Should().HaveCount(5);
        page.HasNextPage.Should().BeTrue();
        page.Items.Select(co => co.Id).Should().BeSubsetOf(choreOccurrences.Select(co => co.Id));
    }

    [Fact]
    public async Task Handle_WhenAfterId_ReturnsChoreOccurrencesPageAfterId()
    {
        await using var ctx = DbFixture.CreateDbContext();
        var user = await Factory.CreateUserAsync(context: ctx);

        var choreOccurrences = new List<ChoreOccurrence>();

        for (int i = 0; i < 10; i++)
        {
            choreOccurrences.Add(await Factory.CreateChoreOccurrenceAsync(user: user, context: ctx));
        }

        var afterId = choreOccurrences[4].Id;
        var request2 = new ListUserChoreOccurrencesRequest(UserId: user.Id, PageSize: 5, AfterId: afterId);
        var result2 = await _handler.Handle(request2);

        result2.IsSuccess.Should().BeTrue();
        var page2 = result2.Value;
        page2.Items.Should().HaveCount(5);
        page2.HasNextPage.Should().BeFalse();
        page2.Items.Select(co => co.Id).Should().OnlyContain(id => id > afterId);
    }

    [Theory]
    [InlineData(ChoreOccurenceFilter.All)]
    [InlineData(ChoreOccurenceFilter.Completed)]
    [InlineData(ChoreOccurenceFilter.Upcoming)]
    [InlineData(ChoreOccurenceFilter.Due)]
    public async Task Handle_WhenFilterApplied_ReturnsFilteredChoreOccurrences(ChoreOccurenceFilter filter)
    {
        // Arrange
        await using var ctx = DbFixture.CreateDbContext();
        var user = await Factory.CreateUserAsync(context: ctx);
        var now = TestClock.UtcNow;

        var completedOccurrences = new List<ChoreOccurrence>();
        var upcomingOccurrences = new List<ChoreOccurrence>();
        var dueOccurrences = new List<ChoreOccurrence>();

        // Create 3 completed chore occurrences (scheduled in past, completed)
        for (int i = 0; i < 3; i++)
        {
            var occurrence = await Factory.CreateChoreOccurrenceAsync(
                user: user,
                scheduledAt: now.AddHours(-1),
                context: ctx
            );
            occurrence.Complete(user.Id, now);
            completedOccurrences.Add(occurrence);
        }

        // Create 3 upcoming (not completed, scheduled in future)
        for (int i = 0; i < 3; i++)
        {
            var occurrence = await Factory.CreateChoreOccurrenceAsync(
                user: user,
                scheduledAt: now.AddDays(1),
                context: ctx
            );
            upcomingOccurrences.Add(occurrence);
        }

        // Create 3 due (not completed, scheduled now or in past)
        for (int i = 0; i < 3; i++)
        {
            var occurrence = await Factory.CreateChoreOccurrenceAsync(
                user: user,
                scheduledAt: now.AddHours(-1),
                context: ctx
            );
            dueOccurrences.Add(occurrence);
        }

        await ctx.SaveChangesAsync();

        var allOccurrences = completedOccurrences
            .Concat(upcomingOccurrences)
            .Concat(dueOccurrences)
            .ToList();

        var request = new ListUserChoreOccurrencesRequest(
            UserId: user.Id,
            PageSize: 100,
            AfterId: null,
            Filter: filter
        );

        // Act
        var result = await _handler.Handle(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var page = result.Value;

        var expectedIds = filter switch
        {
            ChoreOccurenceFilter.All => allOccurrences.Select(co => co.Id),
            ChoreOccurenceFilter.Completed => completedOccurrences.Select(co => co.Id),
            ChoreOccurenceFilter.Upcoming => upcomingOccurrences.Select(co => co.Id),
            ChoreOccurenceFilter.Due => dueOccurrences.Select(co => co.Id),
            _ => throw new ArgumentOutOfRangeException()
        };

        page.Items.Select(co => co.Id).Should().BeEquivalentTo(expectedIds);
    }
}
