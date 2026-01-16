using ChoreNotifier.Features.Notifications.GetNotificationPreference;
using ChoreNotifier.Models;
using FluentAssertions;
using JetBrains.Annotations;

namespace ChoreNotifier.Tests.Features.Notifications.GetNotificationPreference;

[TestSubject(typeof(GetNotificationPreferenceHandler))]
public class GetNotificationPreferenceHandlerTest : DatabaseTestBase
{
    private readonly GetNotificationPreferenceHandler _handler;

    public GetNotificationPreferenceHandlerTest(DatabaseFixture dbFixture, ClockFixture clockFixture)
        : base(dbFixture, clockFixture)
    {
        _handler = new GetNotificationPreferenceHandler(dbFixture.CreateDbContext());
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ReturnsNotFoundError()
    {
        // Arrange
        var nonExistentUserId = 999;

        // Act
        var result = await _handler.Handle(new GetNotificationPreferenceRequest(nonExistentUserId));

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

    [Fact]
    public async Task Handle_WhenUserHasNoPreference_ReturnsNull()
    {
        // Arrange
        var user = await Factory.CreateUserAsync();

        // Act
        var result = await _handler.Handle(new GetNotificationPreferenceRequest(user.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenUserHasConsolePreference_ReturnsConsoleMethodResponse()
    {
        // Arrange
        var user = await Factory.CreateUserAsync();
        var consoleMethod = ConsoleMethod.Create("my-console").Value;

        await using (var context = DbFixture.CreateDbContext())
        {
            var userFromDb = await context.Users.FindAsync(user.Id);
            userFromDb!.NotificationPreference = consoleMethod;
            await context.SaveChangesAsync();
        }

        // Act
        var result = await _handler.Handle(new GetNotificationPreferenceRequest(user.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().BeOfType<ConsoleMethodResponse>();
        var response = (ConsoleMethodResponse)result.Value!;
        response.Name.Should().Be("my-console");
    }

    [Fact]
    public async Task Handle_WhenUserHasNtfyPreference_ReturnsNtfyMethodResponse()
    {
        // Arrange
        var user = await Factory.CreateUserAsync();
        var ntfyMethod = NtfyMethod.Create("my-topic").Value;

        await using (var context = DbFixture.CreateDbContext())
        {
            var userFromDb = await context.Users.FindAsync(user.Id);
            userFromDb!.NotificationPreference = ntfyMethod;
            await context.SaveChangesAsync();
        }

        // Act
        var result = await _handler.Handle(new GetNotificationPreferenceRequest(user.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().BeOfType<NtfyMethodResponse>();
        var response = (NtfyMethodResponse)result.Value!;
        response.TopicName.Should().Be("my-topic");
    }

    [Fact]
    public async Task Handle_WhenMultipleUsersExist_ReturnsCorrectUserPreference()
    {
        // Arrange
        var user1 = await Factory.CreateUserAsync("User 1");
        var user2 = await Factory.CreateUserAsync("User 2");

        await using (var context = DbFixture.CreateDbContext())
        {
            var user1FromDb = await context.Users.FindAsync(user1.Id);
            user1FromDb!.NotificationPreference = ConsoleMethod.Create("console-1").Value;

            var user2FromDb = await context.Users.FindAsync(user2.Id);
            user2FromDb!.NotificationPreference = NtfyMethod.Create("topic-2").Value;

            await context.SaveChangesAsync();
        }

        // Act
        var result = await _handler.Handle(new GetNotificationPreferenceRequest(user2.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeOfType<NtfyMethodResponse>();
        var response = (NtfyMethodResponse)result.Value!;
        response.TopicName.Should().Be("topic-2");
    }
}
