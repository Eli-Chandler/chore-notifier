using ChoreNotifier.Common;

namespace ChoreNotifier.Tests;

public abstract class DatabaseTestBase : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    protected readonly DatabaseFixture DbFixture;
    protected readonly ModelFactory Factory;
    protected readonly TestClock TestClock = new();

    protected DatabaseTestBase(DatabaseFixture dbFixture)
    {
        DbFixture = dbFixture;
        Factory = new ModelFactory(dbFixture);
    }

    public async Task InitializeAsync()
    {
        await DbFixture.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;
}

public class TestClock : IClock
{
    private DateTimeOffset? _fixedTime;

    public DateTimeOffset UtcNow => _fixedTime ?? DateTimeOffset.UtcNow;

    public void SetTime(DateTimeOffset time) => _fixedTime = time;

    public void UseSystemTime() => _fixedTime = null;
}