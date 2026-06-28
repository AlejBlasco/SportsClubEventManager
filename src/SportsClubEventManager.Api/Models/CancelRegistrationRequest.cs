namespace SportsClubEventManager.Api.Models;

/// <summary>
/// Request body for cancelling a user's event registration.
/// </summary>
public sealed record CancelRegistrationRequest
{
    /// <summary>
    /// Gets the unique identifier of the user cancelling their registration.
    /// </summary>
    public required Guid UserId { get; init; }
}
