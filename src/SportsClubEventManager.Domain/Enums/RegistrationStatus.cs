namespace SportsClubEventManager.Domain.Enums;

/// <summary>
/// Represents the status of a registration.
/// </summary>
public enum RegistrationStatus
{
    /// <summary>
    /// The registration is active.
    /// </summary>
    Registered = 0,

    /// <summary>
    /// The registration has been cancelled.
    /// </summary>
    Cancelled = 1,

    /// <summary>
    /// The user is on the waiting list for the event.
    /// </summary>
    Waitlisted = 2
}
