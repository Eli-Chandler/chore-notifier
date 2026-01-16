using ChoreNotifier.Infrastructure.Notifications;
using ChoreNotifier.Models;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace ChoreNotifier.Tests.Infrastructure.Notifications;

// TODO: Make an integration test project
[TestSubject(typeof(NtfyNotificationSender))]
public class NtfyNotificationSenderTest
{
    private NtfyNotificationSender _sender;

    public NtfyNotificationSenderTest()
    {
        var services = new ServiceCollection();

        services.AddHttpClient(); // registers IHttpClientFactory

        var provider = services.BuildServiceProvider();

        var factory = provider.GetRequiredService<IHttpClientFactory>();
        _sender = new NtfyNotificationSender(factory);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SendAsync_WhenValidArgs_SendsNotification()
    {
        // Arrange
        var notification = new Notification("Test Title", "Test Message");
        var methodResult = NtfyMethod.Create("chore-notifier-test-topic");
        methodResult.IsSuccess.Should().BeTrue();
        var method = methodResult.Value;

        // Act
        var result = await _sender.SendNotificationAsync(notification, method);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
