namespace ChoreNotifier.Features.ChoreAlerts;

public class OverdueChoreIndicatorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OverdueChoreIndicatorService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

    public OverdueChoreIndicatorService(
        IServiceProvider serviceProvider,
        ILogger<OverdueChoreIndicatorService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Overdue Chore Indicator Service starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<OverdueChoreIndicatorHandler>();
                await handler.UpdateAlertStateAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating chore alert state");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Overdue Chore Indicator Service stopping");
    }
}
