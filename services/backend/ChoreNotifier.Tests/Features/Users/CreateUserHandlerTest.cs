using ChoreNotifier.Features.Users;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace ChoreNotifier.Tests.Features.Users;

[TestSubject(typeof(CreateUserHandler))]
public class CreateUserHandlerTest: DatabaseTestBase, IClassFixture<DatabaseFixture>
{
    private readonly CreateUserHandler _handler;

    public CreateUserHandlerTest(DatabaseFixture dbFixture) : base(dbFixture)
    {
        _handler = new CreateUserHandler(dbFixture.CreateDbContext(), new CreateUserRequestValidator());
    }
    
    [Fact]
    public async Task WhenValid_CreatesUser()
    {
        // Arrange
        var request = new CreateUserRequest("John Doe");

        // Act
        var response = await _handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotEqual(0, response.Id);
        Assert.Equal("John Doe", response.Name);
        using var context = DbFixture.CreateDbContext();
        var userInDb = await context.Users.FindAsync(response.Id);
        Assert.NotNull(userInDb);
        Assert.Equal("John Doe", userInDb.Name);
    }

    [Fact]
    public async Task WhenNameIsEmpty_ThrowsValidationException()
    {
        // Arrange
        var request = new CreateUserRequest("");

        // Act & Assert
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(async () =>
        {
            await _handler.Handle(request, CancellationToken.None);
        });
    }

    [Fact]
    public async Task WhenNameIsTooLong_ThrowsValidationException()
    {
        // Arrange
        var longName = new string('A', 101);
        var request = new CreateUserRequest(longName);

        // Act & Assert
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(async () =>
        {
            await _handler.Handle(request, CancellationToken.None);
        });
    }
}