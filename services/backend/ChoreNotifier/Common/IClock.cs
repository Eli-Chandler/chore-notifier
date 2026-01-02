namespace ChoreNotifier.Common;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
