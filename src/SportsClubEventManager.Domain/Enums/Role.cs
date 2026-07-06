namespace SportsClubEventManager.Domain.Enums;

/// <summary>
/// Defines the roles available in the system for authorization purposes.
/// </summary>
public enum Role
{
    /// <summary>
    /// Standard user with basic access rights.
    /// Can view events, register for events, and manage their own profile.
    /// </summary>
    User = 0,

    /// <summary>
    /// Administrator with full system access.
    /// Can manage users, events, and access all administrative features.
    /// </summary>
    Administrator = 1
}
