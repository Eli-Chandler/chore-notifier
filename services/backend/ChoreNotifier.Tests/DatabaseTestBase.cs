namespace ChoreNotifier.Tests;

public abstract class DatabaseTestBase : IAsyncLifetime
{
    protected readonly DatabaseFixture Fixture;

    protected DatabaseTestBase(DatabaseFixture fixture)
    {
        Fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await Fixture.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;
}
