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
        _container = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
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

        await using var context = CreateContext();
        await _respawner.ResetAsync(context.Database.GetDbConnection()!);
    }

    /// <summary>
    /// Starts the SQL Server container, applies migrations, and initializes the database reset mechanism.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Apply migrations
        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        // Initialize Respawner
        _respawner = await Respawner.CreateAsync(context.Database.GetDbConnection()!, new RespawnerOptions
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
}
