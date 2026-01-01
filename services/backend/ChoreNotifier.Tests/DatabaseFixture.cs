using ChoreNotifier.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;

namespace ChoreNotifier.Tests;

public class DatabaseFixture : IAsyncLifetime
{
    private static readonly PostgreSqlContainer SharedContainer;
    private static readonly SemaphoreSlim ContainerLock = new(1, 1);
    private static bool _containerStarted;
    
    private readonly string _databaseName;
    private Respawner _respawner = null!;
    private NpgsqlConnection _connection = null!;
    private string _connectionString = null!;

    static DatabaseFixture()
    {
        SharedContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("postgres") // Default admin database
            .WithUsername("test")
            .WithPassword("test")
            .WithReuse(true)
            .WithLabel("chore-notifier-tests", "true")
            .Build();
    }

    public DatabaseFixture()
    {
        // Generate a unique database name for this fixture instance
        _databaseName = $"test_{Guid.NewGuid():N}"[..20]; // Postgres has name length limits
    }

    public async Task InitializeAsync()
    {
        // Ensure container is started (thread-safe, only once)
        await ContainerLock.WaitAsync();
        try
        {
            if (!_containerStarted)
            {
                await SharedContainer.StartAsync();
                _containerStarted = true;
            }
        }
        finally
        {
            ContainerLock.Release();
        }

        // Create a new database for this test collection
        var adminConnectionString = SharedContainer.GetConnectionString();
        await using (var adminConnection = new NpgsqlConnection(adminConnectionString))
        {
            await adminConnection.OpenAsync();
            await using var cmd = adminConnection.CreateCommand();
            cmd.CommandText = $"CREATE DATABASE \"{_databaseName}\"";
            await cmd.ExecuteNonQueryAsync();
        }

        // Build connection string for the new database
        var builder = new NpgsqlConnectionStringBuilder(adminConnectionString)
        {
            Database = _databaseName
        };
        _connectionString = builder.ConnectionString;

        // Create and configure the database context to run migrations
        await using var context = CreateDbContext();
        await context.Database.EnsureCreatedAsync();

        // Initialize Respawn for this database
        _connection = new NpgsqlConnection(_connectionString);
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
        
        // Drop the database when the fixture is disposed
        var adminConnectionString = SharedContainer.GetConnectionString();
        await using var adminConnection = new NpgsqlConnection(adminConnectionString);
        await adminConnection.OpenAsync();
        
        // Terminate existing connections to the database
        await using (var terminateCmd = adminConnection.CreateCommand())
        {
            terminateCmd.CommandText = $"""
                SELECT pg_terminate_backend(pg_stat_activity.pid)
                FROM pg_stat_activity
                WHERE pg_stat_activity.datname = '{_databaseName}'
                AND pid <> pg_backend_pid()
                """;
            await terminateCmd.ExecuteNonQueryAsync();
        }
        
        await using (var dropCmd = adminConnection.CreateCommand())
        {
            dropCmd.CommandText = $"DROP DATABASE IF EXISTS \"{_databaseName}\"";
            await dropCmd.ExecuteNonQueryAsync();
        }
    }
    
    public ChoreDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ChoreDbContext>()
            .UseNpgsql(_connectionString)
            .Options;

        return new ChoreDbContext(options);
    }

    public async Task ResetDatabaseAsync()
    {
        await _respawner.ResetAsync(_connection);
    }
}