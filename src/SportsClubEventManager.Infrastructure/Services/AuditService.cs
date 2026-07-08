using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Domain.Entities;
using SportsClubEventManager.Domain.Enums;

namespace SportsClubEventManager.Infrastructure.Services;

/// <summary>
/// Service for creating audit log entries for administrative actions.
/// </summary>
public class AuditService : IAuditService
{
    private readonly IApplicationDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditService"/> class.
    /// </summary>
    /// <param name="context">The application database context.</param>
    public AuditService(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Logs an administrative action performed on a user.
    /// </summary>
    /// <param name="action">The type of action performed.</param>
    /// <param name="performedByUserId">The ID of the administrator who performed the action.</param>
    /// <param name="targetUserId">The ID of the user who was the target of the action.</param>
    /// <param name="targetUserEmail">The email address of the target user at the time of action.</param>
    /// <param name="changes">Optional JSON representation of the changes made.</param>
    /// <param name="ipAddress">The IP address of the administrator, if available.</param>
    /// <param name="userAgent">The User-Agent string of the client, if available.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task LogAsync(
        AuditAction action,
        Guid performedByUserId,
        Guid targetUserId,
        string targetUserEmail,
        string? changes = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        var auditLog = new AuditLog
        {
            Action = action,
            PerformedByUserId = performedByUserId,
            TargetUserId = targetUserId,
            TargetUserEmail = targetUserEmail,
            Timestamp = DateTime.UtcNow,
            Changes = changes,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };

        _context.AuditLogs.Add(auditLog);

        // Note: SaveChangesAsync is NOT called here. The calling command handler
        // is responsible for saving changes within a single transaction that includes
        // both the business operation and the audit log entry.
        await Task.CompletedTask;
    }
}
