namespace ChoreNotifier.Tests;

public abstract class DatabaseTestBase : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    protected readonly DatabaseFixture DbFixture;
    protected readonly ModelFactory Factory;

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
