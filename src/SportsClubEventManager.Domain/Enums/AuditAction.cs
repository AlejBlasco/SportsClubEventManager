namespace SportsClubEventManager.Domain.Enums;

/// <summary>
/// Defines the types of administrative actions that can be audited.
/// </summary>
public enum AuditAction
{
    /// <summary>
    /// User information was updated by an administrator.
    /// </summary>
    UserUpdated = 0,

    /// <summary>
    /// User account was deactivated by an administrator.
    /// </summary>
    UserDeactivated = 1,

    /// <summary>
    /// User account was activated by an administrator.
    /// </summary>
    UserActivated = 2,

    /// <summary>
    /// Role was assigned to a user by an administrator.
    /// </summary>
    RoleAssigned = 3,

    /// <summary>
    /// Role was removed from a user by an administrator.
    /// </summary>
    RoleRemoved = 4,

    /// <summary>
    /// User account was permanently deleted by an administrator.
    /// </summary>
    UserDeleted = 5
}
