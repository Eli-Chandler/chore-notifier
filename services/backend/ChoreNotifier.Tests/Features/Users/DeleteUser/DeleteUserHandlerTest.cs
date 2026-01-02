using ChoreNotifier.Features.Users.DeleteUser;
using ChoreNotifier.Models;
using FluentAssertions;
using JetBrains.Annotations;

namespace ChoreNotifier.Tests.Features.Users.DeleteUser;

[TestSubject(typeof(DeleteUserHandler))]
public class DeleteUserHandlerTest : DatabaseTestBase
{
    private readonly DeleteUserHandler _handler;

    public DeleteUserHandlerTest(DatabaseFixture dbFixture) : base(dbFixture)
    {
        _handler = new DeleteUserHandler(dbFixture.CreateDbContext());
    }

    [Fact]
    public async Task Handle_WhenUserExists_DeletesUser()
    {
        // Arrange
        var user = await Factory.CreateUserAsync("Test User");

        // Act
        var result = await _handler.Handle(user.Id, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        using var context = DbFixture.CreateDbContext();
        var userInDb = await context.Users.FindAsync(user.Id);
        userInDb.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ReturnsNotFoundError()
    {
        // Arrange & Act
        var result = await _handler.Handle(9999, CancellationToken.None);

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
                e.EntityName == "User" &&
                (string)e.EntityKey == "9999");
    }

    [Fact]
    public async Task Handle_WhenMultipleUsersExist_DeletesOnlySpecifiedUser()
    {
        // Arrange
        var users = await Factory.CreateUsersAsync(3);
        var userToDelete = users[1];

        // Act
        var result = await _handler.Handle(userToDelete.Id, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        using var context = DbFixture.CreateDbContext();
        var deletedUser = await context.Users.FindAsync(userToDelete.Id);
        deletedUser.Should().BeNull();

        var remainingUser1 = await context.Users.FindAsync(users[0].Id);
        remainingUser1.Should().NotBeNull();

        var remainingUser2 = await context.Users.FindAsync(users[2].Id);
        remainingUser2.Should().NotBeNull();
    }
}