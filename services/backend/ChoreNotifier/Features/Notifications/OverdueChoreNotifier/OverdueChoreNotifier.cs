namespace ChoreNotifier.Features.Notifications.OverdueChoreNotifier;

public class OverdueChoreNotifier : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OverdueChoreNotifier> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(3);

    public OverdueChoreNotifier(
        IServiceProvider serviceProvider,
        ILogger<OverdueChoreNotifier> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Overdue Chore Notifier starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<OverdueChoreNotificationHandler>();
                await handler.CheckAndNotifyOverdueChoresAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for overdue chores");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Overdue Chore Notifier stopping");
    }
}
