using Microsoft.EntityFrameworkCore;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Domain.Entities;

namespace SportsClubEventManager.Application.Tests.Common;

/// <summary>
/// Factory for creating in-memory test database contexts.
/// </summary>
internal sealed class TestDbContextFactory
{
    /// <summary>
    /// Creates an in-memory database context for testing.
    /// </summary>
    /// <returns>A configured IApplicationDbContext instance with in-memory database.</returns>
    public static IApplicationDbContext CreateTestContext()
    {
        var options = new DbContextOptionsBuilder<TestApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TestApplicationDbContext(options);
    }

    /// <summary>
    /// Creates an in-memory database context with pre-seeded events.
    /// </summary>
    /// <param name="events">Events to seed into the context.</param>
    /// <returns>A configured IApplicationDbContext instance with seeded data.</returns>
    public static IApplicationDbContext CreateTestContextWithEvents(List<Event> events)
    {
        var context = CreateTestContext();
        context.Events.AddRange(events);
        context.SaveChangesAsync(CancellationToken.None).GetAwaiter().GetResult();
        return context;
    }
}

/// <summary>
/// Test implementation of IApplicationDbContext using in-memory database.
/// </summary>
internal sealed class TestApplicationDbContext : DbContext, IApplicationDbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TestApplicationDbContext"/> class.
    /// </summary>
    /// <param name="options">The DbContext options.</param>
    public TestApplicationDbContext(DbContextOptions options) : base(options)
    {
    }

    /// <summary>
    /// Gets the Events table.
    /// </summary>
    public DbSet<Event> Events => Set<Event>();

    /// <summary>
    /// Gets the Registrations table.
    /// </summary>
    public DbSet<Registration> Registrations => Set<Registration>();

    /// <summary>
    /// Gets the Users table.
    /// </summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>
    /// Gets the AuditLogs table.
    /// </summary>
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
}
