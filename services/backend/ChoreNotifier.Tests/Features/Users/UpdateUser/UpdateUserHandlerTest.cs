using ChoreNotifier.Features.Users.UpdateUser;
using ChoreNotifier.Models;
using FluentAssertions;
using JetBrains.Annotations;

namespace ChoreNotifier.Tests.Features.Users.UpdateUser;

[TestSubject(typeof(UpdateUserHandler))]
public class UpdateUserHandlerTest : DatabaseTestBase
{
    private readonly UpdateUserHandler _handler;

    public UpdateUserHandlerTest(DatabaseFixture dbFixture) : base(dbFixture)
    {
        _handler = new UpdateUserHandler(dbFixture.CreateDbContext());
    }

    [Fact]
    public async Task Handle_WhenUserExists_UpdatesUser()
    {
        // Arrange
        var user = await Factory.CreateUserAsync("Original Name");
        var request = new UpdateUserRequest { Name = "Updated Name" };

        // Act
        var result = await _handler.Handle(user.Id, request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(user.Id);
        result.Value.Name.Should().Be("Updated Name");

        using var context = DbFixture.CreateDbContext();
        var userInDb = await context.Users.FindAsync(user.Id);
        userInDb.Should().NotBeNull();
        userInDb!.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ReturnsNotFoundError()
    {
        // Arrange
        var request = new UpdateUserRequest { Name = "Test Name" };

        // Act
        var result = await _handler.Handle(9999, request, CancellationToken.None);

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
    public async Task Handle_WhenNameIsEmpty_ReturnsError()
    {
        // Arrange
        var user = await Factory.CreateUserAsync("Original Name");
        var request = new UpdateUserRequest { Name = "" };

        // Act
        var result = await _handler.Handle(user.Id, request, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors
            .Should()
            .ContainSingle()
            .Which
            .Message.Should().Contain("Name is required");
    }

    [Fact]
    public async Task Handle_WhenNameIsTooLong_ReturnsError()
    {
        // Arrange
        var user = await Factory.CreateUserAsync("Original Name");
        var longName = new string('A', 101);
        var request = new UpdateUserRequest { Name = longName };

        // Act
        var result = await _handler.Handle(user.Id, request, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors
            .Should()
            .ContainSingle()
            .Which
            .Message.Should().Contain("Name cannot exceed 100 characters");
    }
}