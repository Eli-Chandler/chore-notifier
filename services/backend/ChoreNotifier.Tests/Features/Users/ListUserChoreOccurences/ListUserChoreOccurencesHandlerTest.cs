using ChoreNotifier.Features.Users.ListUserChoreOccurences;
using ChoreNotifier.Models;
using FluentAssertions;
using JetBrains.Annotations;

namespace ChoreNotifier.Tests.Features.Users.ListUserChoreOccurences;

[TestSubject(typeof(ListUserChoreOccurencesHandler))]
public class ListUserChoreOccurencesHandlerTest : DatabaseTestBase
{
    private readonly ListUserChoreOccurencesHandler _handler;

    public ListUserChoreOccurencesHandlerTest(DatabaseFixture dbFixture, ClockFixture clockFixture) : base(dbFixture,
        clockFixture)
    {
        _handler = new ListUserChoreOccurencesHandler(dbFixture.CreateDbContext(), clockFixture.TestClock);
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

    [Fact]
    public async Task Handle_WhenChoreOccurrencesDueAtDifferentTimes_ReturnsInCorrectOrder()
    {


        // Arrange
        await using var ctx = DbFixture.CreateDbContext();
        var user = await Factory.CreateUserAsync(context: ctx);

        var choreOccurrences = new List<ChoreOccurrence>();

        for (int i = 0; i < 10; i++)
        {
            var co = await Factory.CreateChoreOccurrenceAsync(
                user: user,
                scheduledAt: TestClock.UtcNow.AddDays(i),
                context: ctx);

            if (i % 2 == 0)
            {
                co.Complete(user.Id, TestClock.UtcNow.AddDays(i + 1));
            }
            choreOccurrences.Add(co);
        }

        await ctx.SaveChangesAsync();

        // Act
        var request = new ListUserChoreOccurrencesRequest(UserId: user.Id, PageSize: 10, AfterId: null);
        var result = await _handler.Handle(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var page = result.Value;
        page.Items.Should().HaveCount(10);
        // Should be ordered first by:
        // Completed or not? (Incomplete first)
        // Then by Due date (earliest first)
        // With a final tiebreaker by Id (lowest first)
        var expectedOrder = choreOccurrences
            .OrderByDescending(co => co.IsCompleted)
            .ThenBy(co => co.DueAt)
            .ThenBy(co => co.Id)
            .Select(co => co.Id)
            .ToList();

        page.Items.Select(co => co.Id).Should().Equal(expectedOrder);
    }
}
