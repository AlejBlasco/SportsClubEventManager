using MediatR;

namespace SportsClubEventManager.Application.Events.Commands.CancelRegistration;

/// <summary>
/// Command to cancel a user's registration for a specific event.
/// </summary>
public sealed record CancelRegistrationCommand : IRequest
{
    /// <summary>
    /// Gets the unique identifier of the event.
    /// </summary>
    public required Guid EventId { get; init; }

    /// <summary>
    /// Gets the unique identifier of the user cancelling their registration.
    /// </summary>
    public required Guid UserId { get; init; }
}
