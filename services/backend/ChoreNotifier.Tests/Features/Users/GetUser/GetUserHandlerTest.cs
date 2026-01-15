using ChoreNotifier.Features.Users.GetUser;
using FluentAssertions;
using JetBrains.Annotations;

namespace ChoreNotifier.Tests.Features.Users.GetUser;

[TestSubject(typeof(GetUserHandler))]
public class GetUserHandlerTest : DatabaseTestBase
{
    private readonly GetUserHandler _handler;

    public GetUserHandlerTest(DatabaseFixture dbFixture, ClockFixture clockFixture) : base(dbFixture, clockFixture)
    {
        _handler = new GetUserHandler(dbFixture.CreateDbContext());
    }

    [Fact]
    public async Task Handle_WhenUserExists_ReturnsUser()
    {
        // Arrange
        var user = await Factory.CreateUserAsync("Test User");

        // Act
        var result = await _handler.Handle(new GetUserRequest(user.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(user.Id);
        result.Value.Name.Should().Be("Test User");
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ReturnsNotFoundError()
    {
        // Arrange
        var nonExistentUserId = 999;

        // Act
        var result = await _handler.Handle(new GetUserRequest(nonExistentUserId));

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors
            .Should()
            .ContainSingle()
            .Which
            .Message.Should().Contain("User")
            .And.Contain(nonExistentUserId.ToString())
            .And.Contain("was not found");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Handle_WhenMultipleUsersExist_ReturnsCorrectUser(int userIndex)
    {
        // Arrange
        var users = await Factory.CreateUsersAsync(3, i => $"User {i}");
        var targetUser = users[userIndex];

        // Act
        var result = await _handler.Handle(new GetUserRequest(targetUser.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(targetUser.Id);
        result.Value.Name.Should().Be(targetUser.Name);
    }
}
