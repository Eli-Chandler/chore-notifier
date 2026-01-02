using ChoreNotifier.Features.Chores.ListChores;
using FluentAssertions;
using JetBrains.Annotations;

namespace ChoreNotifier.Tests.Features.Chores.ListChores;

[TestSubject(typeof(ListChoresHandler))]
public class ListChoresHandlerTest : DatabaseTestBase
{
    private readonly ListChoresHandler _handler;

    public ListChoresHandlerTest(DatabaseFixture dbFixture) : base(dbFixture)
    {
        _handler = new ListChoresHandler(dbFixture.CreateDbContext());
    }

    [Fact]
    public async Task Handle_WhenNoChores_ReturnsEmptyList()
    {
        // Arrange & Act
        var result = await _handler.Handle(10, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.HasNextPage.Should().BeFalse();
        result.Value.NextCursor.Should().NotHaveValue();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task Handle_WhenChoresExist_ReturnsAllChores(int choreCount)
    {
        // Arrange
        var chores = await Factory.CreateChoresAsync(choreCount, i => $"Chore {i}");

        // Act
        var result = await _handler.Handle(20, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(choreCount);
        result.Value.HasNextPage.Should().BeFalse();
        result.Value.NextCursor.Should().NotHaveValue();

        foreach (var chore in chores)
        {
            result.Value.Items.Should().Contain(c =>
                c.Id == chore.Id &&
                c.Title == chore.Title &&
                c.Description == chore.Description &&
                c.SnoozeDuration == chore.SnoozeDuration);
        }
    }

    [Fact]
    public async Task Handle_WhenPageSizeSmallerThanTotal_ReturnsPaginatedResults()
    {
        // Arrange
        var chores = await Factory.CreateChoresAsync(10);

        // Act
        var result = await _handler.Handle(5, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
        result.Value.HasNextPage.Should().BeTrue();
        result.Value.NextCursor.Should().HaveValue();
        result.Value.NextCursor.Should().Be(chores[4].Id);
    }

    [Fact]
    public async Task Handle_WhenUsingCursor_ReturnsRemainingChores()
    {
        // Arrange
        var chores = await Factory.CreateChoresAsync(10);

        // Act - Get first page
        var firstPage = await _handler.Handle(5, null);

        // Act - Get second page using cursor
        var secondPage = await _handler.Handle(5, firstPage.Value.NextCursor);

        // Assert
        firstPage.Value.Items.Should().HaveCount(5);
        secondPage.Value.Items.Should().HaveCount(5);
        secondPage.Value.HasNextPage.Should().BeFalse();
        secondPage.Value.NextCursor.Should().NotHaveValue();

        var allReturnedIds = firstPage.Value.Items.Select(c => c.Id)
            .Concat(secondPage.Value.Items.Select(c => c.Id))
            .OrderBy(id => id);

        var expectedIds = chores.Select(c => c.Id).OrderBy(id => id);
        allReturnedIds.Should().BeEquivalentTo(expectedIds);
    }

    [Fact]
    public async Task Handle_WhenPageSizeIsZero_ReturnsError()
    {
        // Arrange & Act
        var result = await _handler.Handle(0, null);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors
            .Should()
            .ContainSingle()
            .Which
            .Message.Should().Contain("Page size must be greater than 0");
    }

    [Fact]
    public async Task Handle_WhenPageSizeIsNegative_ReturnsError()
    {
        // Arrange & Act
        var result = await _handler.Handle(-1, null);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors
            .Should()
            .ContainSingle()
            .Which
            .Message.Should().Contain("Page size must be greater than 0");
    }

    [Fact]
    public async Task Handle_WhenPageSizeExceeds100_ReturnsError()
    {
        // Arrange & Act
        var result = await _handler.Handle(101, null);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors
            .Should()
            .ContainSingle()
            .Which
            .Message.Should().Contain("Page size cannot exceed 100");
    }
}