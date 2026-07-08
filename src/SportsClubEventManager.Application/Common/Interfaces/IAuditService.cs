using SportsClubEventManager.Domain.Enums;

namespace SportsClubEventManager.Application.Common.Interfaces;

/// <summary>
/// Defines the contract for audit logging operations.
/// </summary>
public interface IAuditService
{
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
    Task LogAsync(
        AuditAction action,
        Guid performedByUserId,
        Guid targetUserId,
        string targetUserEmail,
        string? changes = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);
}
