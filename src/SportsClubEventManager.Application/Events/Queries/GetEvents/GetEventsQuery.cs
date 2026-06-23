using MediatR;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Application.Events.Queries.GetEvents;

/// <summary>
/// Query to retrieve a list of events with optional date filtering.
/// </summary>
public sealed record GetEventsQuery : IRequest<List<EventDto>>
{
    /// <summary>
    /// Gets the optional start date filter (inclusive).
    /// Only events on or after this date will be returned.
    /// </summary>
    public DateTime? StartDate { get; init; }

    /// <summary>
    /// Gets the optional end date filter (inclusive).
    /// Only events on or before this date will be returned.
    /// </summary>
    public DateTime? EndDate { get; init; }
}
