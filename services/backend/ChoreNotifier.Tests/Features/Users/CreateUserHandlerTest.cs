using ChoreNotifier.Data;
using ChoreNotifier.Features.Users;
using JetBrains.Annotations;

namespace ChoreNotifier.Tests.Features.Users;

[TestSubject(typeof(CreateUserHandler))]
public class CreateUserHandlerTest: DatabaseTestBase, IClassFixture<DatabaseFixture>
{
    private readonly ChoreDbContext _context;
    private readonly CreateUserHandler _handler;

    public CreateUserHandlerTest(DatabaseFixture fixture) : base(fixture)
    {
        _context = Fixture.CreateDbContext();
        _handler = new CreateUserHandler(_context, new CreateUserRequestValidator());
    }
    
    [Fact]
    public async Task Handle_WhenValid_CreatesUser()
    {
        // Arrange
        var request = new CreateUserRequest("John Doe");

        // Act
        var response = await _handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotEqual(0, response.Id);
        Assert.Equal("John Doe", response.Name);
    }

    [Fact]
    public async Task Handle_WhenNameIsEmpty_ThrowsValidationException()
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
    public async Task Handle_WhenNameIsTooLong_ThrowsValidationException()
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