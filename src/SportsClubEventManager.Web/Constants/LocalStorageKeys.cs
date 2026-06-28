namespace SportsClubEventManager.Web.Constants;

/// <summary>
/// Defines constant keys used for browser localStorage operations.
/// Centralizes key naming to prevent duplication and typos.
/// </summary>
public static class LocalStorageKeys
{
    /// <summary>
    /// Prefix for event registration data storage keys.
    /// Full key format: event_registration_{eventId}
    /// </summary>
    public const string EventRegistrationPrefix = "event_registration_";
}
