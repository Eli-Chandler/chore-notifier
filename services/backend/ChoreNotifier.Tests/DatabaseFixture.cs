using ChoreNotifier.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;

namespace ChoreNotifier.Tests;

public class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;
    private Respawner _respawner = null!;
    private NpgsqlConnection _connection = null!;

    public DatabaseFixture()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("chorenotifier_test")
            .WithUsername("test")
            .WithPassword("test")
            .WithReuse(true)
            .Build();
    }

    public async Task InitializeAsync()
    {
        // Start the container
        await _container.StartAsync();

        // Create and configure the database context to run migrations
        await using var context = CreateDbContext();
        await context.Database.EnsureCreatedAsync();

        // Initialize Respawn
        _connection = new NpgsqlConnection(_container.GetConnectionString());
        await _connection.OpenAsync();

        _respawner = await Respawner.CreateAsync(_connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"]
        });
    }

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
        // Don't dispose the container to allow for reuse
        // await _container.DisposeAsync();
    }
    
    public ChoreDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ChoreDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        return new ChoreDbContext(options);
    }

    public async Task ResetDatabaseAsync()
    {
        await _respawner.ResetAsync(_connection);
    }
}