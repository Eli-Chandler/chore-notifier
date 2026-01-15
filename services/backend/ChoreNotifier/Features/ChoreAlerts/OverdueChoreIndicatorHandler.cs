using ChoreNotifier.Data;
using ChoreNotifier.Infrastructure.ChoreAlerts;
using ChoreNotifier.Infrastructure.Clock;
using Microsoft.EntityFrameworkCore;

namespace ChoreNotifier.Features.ChoreAlerts;

public class OverdueChoreIndicatorHandler
{
    private readonly ChoreDbContext _db;
    private readonly IChoreAlertIndicator _alertIndicator;
    private readonly IClock _clock;
    private readonly ILogger<OverdueChoreIndicatorHandler> _logger;

    public OverdueChoreIndicatorHandler(
        ChoreDbContext db,
        IChoreAlertIndicator alertIndicator,
        IClock clock,
        ILogger<OverdueChoreIndicatorHandler> logger)
    {
        _db = db;
        _alertIndicator = alertIndicator;
        _clock = clock;
        _logger = logger;
    }

    public async Task UpdateAlertStateAsync(CancellationToken ct = default)
    {
        var currentTime = _clock.UtcNow;

        var hasOverdueChores = await _db.ChoreOccurrences
            .AnyAsync(co => co.CompletedAt == null && co.DueAt <= currentTime, ct);

        var newState = hasOverdueChores ? ChoreAlertState.Overdue : ChoreAlertState.Ok;

        _logger.LogDebug("Setting chore alert state to {State}", newState);
        await _alertIndicator.SetStateAsync(newState, ct);
    }
}
