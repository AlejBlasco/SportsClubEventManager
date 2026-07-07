using Microsoft.EntityFrameworkCore;
using SportsClubEventManager.Domain.Entities;

namespace SportsClubEventManager.Application.Common.Interfaces;

/// <summary>
/// Defines the contract for the application database context.
/// </summary>
public interface IApplicationDbContext
{
    /// <summary>
    /// Gets the Events table.
    /// </summary>
    DbSet<Event> Events { get; }

    /// <summary>
    /// Gets the Registrations table.
    /// </summary>
    DbSet<Registration> Registrations { get; }

    /// <summary>
    /// Gets the Users table.
    /// </summary>
    DbSet<User> Users { get; }

    /// <summary>
    /// Gets the AuditLogs table.
    /// </summary>
    DbSet<AuditLog> AuditLogs { get; }

    /// <summary>
    /// Saves all changes made in this context to the database asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous save operation. The task result contains the number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
