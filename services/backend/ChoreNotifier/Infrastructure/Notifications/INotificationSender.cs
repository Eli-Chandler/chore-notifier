using System.Text;
using ChoreNotifier.Models;
using FluentResults;

namespace ChoreNotifier.Infrastructure.Notifications;

public interface INotificationSender
{
    NotificationType Type { get; }
    Task<Result> SendNotificationAsync(Notification notification, NotificationMethod method);
}

public abstract class NotificationSender<TMethod> : INotificationSender
    where TMethod : NotificationMethod
{
    public abstract NotificationType Type { get; }

    public Task<Result> SendNotificationAsync(Notification notification, NotificationMethod method)
    {
        if (method is not TMethod typedMethod)
            throw new ArgumentException(
                $"Invalid method type: expected {typeof(TMethod)}, got {method.GetType()}");

        return SendAsync(notification, typedMethod);
    }

    protected abstract Task<Result> SendAsync(Notification notification, TMethod method);
}

public class ConsoleNotificationSender : NotificationSender<ConsoleMethod>
{
    public override NotificationType Type => NotificationType.Console;

    protected override Task<Result> SendAsync(Notification notification, ConsoleMethod method)
    {
        Console.WriteLine($"[{method.Name}] {notification.Title}: {notification.Message}");
        return Task.FromResult(Result.Ok());
    }
}

public class NtfyNotificationSender : NotificationSender<NtfyMethod>
{
    public override NotificationType Type => NotificationType.Ntfy;

    private readonly HttpClient _http;
    private const string ServerUrl = "https://ntfy.sh/";

    public NtfyNotificationSender(IHttpClientFactory httpClientFactory)
        => _http = httpClientFactory.CreateClient(nameof(NtfyNotificationSender));


    protected override async Task<Result> SendAsync(Notification notification, NtfyMethod method)
    {
        var baseUri = new Uri(ServerUrl, UriKind.Absolute);
        var encodedTopic = Uri.EscapeDataString(method.TopicName);
        var publishUri = new Uri(baseUri, encodedTopic);

        using var request = new HttpRequestMessage(HttpMethod.Post, publishUri)
        {
            Content = new StringContent(notification.Message, Encoding.UTF8, "text/plain")
        };

        if (!string.IsNullOrWhiteSpace(notification.Title))
            request.Headers.TryAddWithoutValidation("Title", notification.Title);

        using var response = await _http.SendAsync(request);

        if (response.IsSuccessStatusCode)
            return Result.Ok();

        var errorBody = await response.Content.ReadAsStringAsync();

        return response.IsSuccessStatusCode
            ? Result.Ok()
            : Result.Fail($"ntfy request failed ({(int)response.StatusCode}): {errorBody}");
    }
}
