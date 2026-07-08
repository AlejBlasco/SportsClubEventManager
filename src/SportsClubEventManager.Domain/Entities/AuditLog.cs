using SportsClubEventManager.Domain.Common;
using SportsClubEventManager.Domain.Enums;

namespace SportsClubEventManager.Domain.Entities;

/// <summary>
/// Represents an audit log entry for administrative actions performed on users.
/// </summary>
public class AuditLog : BaseEntity
{
    /// <summary>
    /// Gets or sets the type of action performed.
    /// </summary>
    public AuditAction Action { get; set; }

    /// <summary>
    /// Gets or sets the ID of the administrator who performed the action.
    /// </summary>
    public Guid PerformedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the administrator who performed the action.
    /// </summary>
    public User PerformedByUser { get; set; } = null!;

    /// <summary>
    /// Gets or sets the ID of the user who was the target of the action.
    /// </summary>
    public Guid TargetUserId { get; set; }

    /// <summary>
    /// Gets or sets the email address of the target user at the time of the action.
    /// Captured for audit trail integrity even if the user is later deleted.
    /// </summary>
    public string TargetUserEmail { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the action was performed.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the JSON serialized representation of the changes made.
    /// Contains old and new values for modified fields.
    /// </summary>
    public string? Changes { get; set; }

    /// <summary>
    /// Gets or sets the IP address of the administrator performing the action.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the User-Agent string of the browser/client used to perform the action.
    /// </summary>
    public string? UserAgent { get; set; }
}
