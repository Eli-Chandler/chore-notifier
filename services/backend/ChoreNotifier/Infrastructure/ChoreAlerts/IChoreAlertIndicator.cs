namespace ChoreNotifier.Infrastructure.ChoreAlerts;

public enum ChoreAlertState
{
    Ok,
    Overdue
}

public interface IChoreAlertIndicator
{
    Task SetStateAsync(ChoreAlertState state, CancellationToken ct = default);
}
