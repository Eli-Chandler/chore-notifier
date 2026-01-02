using ChoreNotifier.Features.Users.CreateUser;
using FluentAssertions;
using JetBrains.Annotations;

namespace ChoreNotifier.Tests.Features.Users.CreateUser;

[TestSubject(typeof(CreateUserHandler))]
public class CreateUserHandlerTest : DatabaseTestBase
{
    private readonly CreateUserHandler _handler;

    public CreateUserHandlerTest(DatabaseFixture dbFixture) : base(dbFixture)
    {
        _handler = new CreateUserHandler(dbFixture.CreateDbContext());
    }

    [Fact]
    public async Task Handle_WhenValid_CreatesUser()
    {
        // Arrange
        var request = new CreateUserRequest("John Doe");

        // Act
        var result = await _handler.Handle(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().NotBe(0);
        result.Value.Name.Should().Be("John Doe");

        using var context = DbFixture.CreateDbContext();
        var userInDb = await context.Users.FindAsync(result.Value.Id);
        userInDb.Should().NotBeNull();
        userInDb!.Name.Should().Be("John Doe");
    }

    [Fact]
    public async Task Handle_WhenNameIsEmpty_ReturnsError()
    {
        // Arrange
        var request = new CreateUserRequest("");

        // Act
        var result = await _handler.Handle(request);

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
        var longName = new string('A', 101);
        var request = new CreateUserRequest(longName);

        // Act
        var result = await _handler.Handle(request);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors
            .Should()
            .ContainSingle()
            .Which
            .Message.Should().Contain("Name cannot exceed 100 characters");
    }
}