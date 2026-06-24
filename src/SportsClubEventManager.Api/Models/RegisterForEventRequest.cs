namespace SportsClubEventManager.Api.Models;

/// <summary>
/// Request body for registering a user for an event.
/// </summary>
public sealed record RegisterForEventRequest
{
    /// <summary>
    /// Gets the unique identifier of the user registering for the event.
    /// </summary>
    public required Guid UserId { get; init; }
}
