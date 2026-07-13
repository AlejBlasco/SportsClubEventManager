using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Respawn;
using SportsClubEventManager.Infrastructure.Persistence;
using Testcontainers.MsSql;

namespace SportsClubEventManager.IntegrationTests;

/// <summary>
/// Provides a SQL Server container and database reset functionality for integration tests.
/// </summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container;
    private Respawner? _respawner;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseFixture"/> class.
    /// </summary>
    public DatabaseFixture()
    {
        _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("YourStrong!Passw0rd")
            .Build();
    }

    /// <summary>
    /// Gets the connection string for the SQL Server test container.
    /// </summary>
    public string ConnectionString => _container.GetConnectionString();

    /// <summary>
    /// Creates a new AppDbContext connected to the test database.
    /// </summary>
    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new AppDbContext(options);
    }

    /// <summary>
    /// Resets the database to a clean state, removing all data but preserving schema.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        if (_respawner is null)
        {
            throw new InvalidOperationException("Database has not been initialized. Call InitializeAsync first.");
        }

        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
    }

    /// <summary>
    /// Starts the SQL Server container, applies migrations, and initializes the database reset mechanism.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.SqlServer
        });
    }

    /// <summary>
    /// Stops and disposes the SQL Server container.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    /// <summary>
    /// Stops the SQL Server container without disposing this fixture, simulating a database
    /// outage that occurs after the application has already started successfully (as opposed to
    /// the database being unreachable from the very first connection attempt). Used by tests that
    /// need to observe a health check transitioning from Healthy to Unhealthy mid-test; the
    /// container is not restarted afterwards, so this should only be called as the last action of
    /// a test (issue #41).
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task StopContainerAsync()
    {
        await _container.StopAsync();
    }
}
