using ChoreNotifier.Features.Users.ListUsers;
using FluentAssertions;
using JetBrains.Annotations;

namespace ChoreNotifier.Tests.Features.Users.ListUsers;

[TestSubject(typeof(ListUsersHandler))]
public class ListUsersHandlerTest : DatabaseTestBase
{
    private readonly ListUsersHandler _handler;

    public ListUsersHandlerTest(DatabaseFixture dbFixture) : base(dbFixture)
    {
        _handler = new ListUsersHandler(dbFixture.CreateDbContext());
    }

    [Fact]
    public async Task Handle_WhenNoUsers_ReturnsEmptyList()
    {
        // Arrange & Act
        var result = await _handler.Handle(10, null, CancellationToken.None);

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
    public async Task Handle_WhenUsersExist_ReturnsAllUsers(int userCount)
    {
        // Arrange
        var users = await Factory.CreateUsersAsync(userCount, i => $"User {i}");

        // Act
        var result = await _handler.Handle(20, null, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(userCount);
        result.Value.HasNextPage.Should().BeFalse();
        result.Value.NextCursor.Should().NotHaveValue();

        foreach (var user in users)
        {
            result.Value.Items.Should().Contain(u => u.Id == user.Id && u.Name == user.Name);
        }
    }

    [Fact]
    public async Task Handle_WhenPageSizeSmallerThanTotal_ReturnsPaginatedResults()
    {
        // Arrange
        var users = await Factory.CreateUsersAsync(10);

        // Act
        var result = await _handler.Handle(5, null, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
        result.Value.HasNextPage.Should().BeTrue();
        result.Value.NextCursor.Should().HaveValue();
        result.Value.NextCursor.Should().Be(users[4].Id);
    }

    [Fact]
    public async Task Handle_WhenUsingCursor_ReturnsRemainingUsers()
    {
        // Arrange
        var users = await Factory.CreateUsersAsync(10);

        // Act - Get first page
        var firstPage = await _handler.Handle(5, null, CancellationToken.None);

        // Act - Get second page using cursor
        var secondPage = await _handler.Handle(5, firstPage.Value.NextCursor, CancellationToken.None);

        // Assert
        firstPage.Value.Items.Should().HaveCount(5);
        secondPage.Value.Items.Should().HaveCount(5);
        secondPage.Value.HasNextPage.Should().BeFalse();
        secondPage.Value.NextCursor.Should().NotHaveValue();

        var allReturnedIds = firstPage.Value.Items.Select(u => u.Id)
            .Concat(secondPage.Value.Items.Select(u => u.Id))
            .OrderBy(id => id);

        var expectedIds = users.Select(u => u.Id).OrderBy(id => id);
        allReturnedIds.Should().BeEquivalentTo(expectedIds);
    }

    [Fact]
    public async Task Handle_WhenPageSizeIsZero_ReturnsError()
    {
        // Arrange & Act
        var result = await _handler.Handle(0, null, CancellationToken.None);

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
        var result = await _handler.Handle(-1, null, CancellationToken.None);

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
        var result = await _handler.Handle(101, null, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors
            .Should()
            .ContainSingle()
            .Which
            .Message.Should().Contain("Page size cannot exceed 100");
    }
}