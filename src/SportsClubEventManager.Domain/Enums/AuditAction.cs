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
    UserDeleted = 5,

    /// <summary>
    /// Event was created by an administrator.
    /// </summary>
    EventCreated = 6,

    /// <summary>
    /// Event was updated by an administrator.
    /// </summary>
    EventUpdated = 7,

    /// <summary>
    /// Event was deleted by an administrator.
    /// </summary>
    EventDeleted = 8,

    /// <summary>
    /// Registration was created by an administrator.
    /// </summary>
    RegistrationCreated = 9,

    /// <summary>
    /// Registration was cancelled by an administrator.
    /// </summary>
    RegistrationCancelled = 10,

    /// <summary>
    /// A batch of events was created by an administrator via CSV import.
    /// </summary>
    EventsImported = 11
}
