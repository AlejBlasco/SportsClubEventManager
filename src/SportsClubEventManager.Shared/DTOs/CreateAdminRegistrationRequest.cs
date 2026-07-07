namespace SportsClubEventManager.Shared.DTOs;

/// <summary>
/// Request payload for manual registration creation by administrators.
/// </summary>
public sealed class CreateAdminRegistrationRequest
{
    /// <summary>
    /// Gets or sets the user identifier to register.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the event identifier where the user will be registered.
    /// </summary>
    public Guid EventId { get; set; }
}
