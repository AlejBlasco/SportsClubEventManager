using MediatR;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Application.Events.Queries.GetEventById;

/// <summary>
/// Query to retrieve detailed information about a specific event by its identifier.
/// </summary>
public sealed record GetEventByIdQuery : IRequest<EventDetailDto?>
{
    /// <summary>
    /// Gets the unique identifier of the event to retrieve.
    /// </summary>
    public required Guid EventId { get; init; }
}
