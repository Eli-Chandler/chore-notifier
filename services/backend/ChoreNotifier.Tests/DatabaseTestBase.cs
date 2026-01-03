using ChoreNotifier.Common;

namespace ChoreNotifier.Tests;

public abstract class DatabaseTestBase : IClassFixture<DatabaseFixture>, IClassFixture<ClockFixture>, IAsyncLifetime
{
    protected readonly DatabaseFixture DbFixture;
    protected readonly ModelFactory Factory;
    protected readonly TestClock TestClock;

    protected DatabaseTestBase(DatabaseFixture dbFixture, ClockFixture clockFixture)
    {
        DbFixture = dbFixture;
        TestClock = clockFixture.TestClock;
        Factory = new ModelFactory(dbFixture, clockFixture.TestClock);
    }

    public async Task InitializeAsync()
    {
        await DbFixture.ResetDatabaseAsync();
        TestClock.UseSystemTime(); // Reset clock between tests
    }

    public Task DisposeAsync() => Task.CompletedTask;
}

public class ClockFixture
{
    public TestClock TestClock { get; } = new();
}

public class TestClock : IClock
{
    private DateTimeOffset? _fixedTime;

    public DateTimeOffset UtcNow => _fixedTime ?? DateTimeOffset.UtcNow;

    public void SetTime(DateTimeOffset time) => _fixedTime = time;

    public void FreezeTime() => _fixedTime = DateTimeOffset.UtcNow;

    public void UseSystemTime() => _fixedTime = null;
}
