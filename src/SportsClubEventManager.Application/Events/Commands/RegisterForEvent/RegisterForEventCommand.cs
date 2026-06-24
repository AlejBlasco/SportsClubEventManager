using MediatR;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Application.Events.Commands.RegisterForEvent;

/// <summary>
/// Command to register a user for a specific event.
/// </summary>
public sealed record RegisterForEventCommand : IRequest<RegistrationCreatedDto>
{
    /// <summary>
    /// Gets the unique identifier of the event to register for.
    /// </summary>
    public required Guid EventId { get; init; }

    /// <summary>
    /// Gets the unique identifier of the user registering for the event.
    /// </summary>
    public required Guid UserId { get; init; }
}
