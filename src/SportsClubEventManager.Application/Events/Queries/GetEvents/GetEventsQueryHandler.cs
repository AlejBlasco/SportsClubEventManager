using MediatR;
using Microsoft.EntityFrameworkCore;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Domain.Enums;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Application.Events.Queries.GetEvents;

/// <summary>
/// Handles the GetEventsQuery by retrieving events from the database with optional filtering.
/// </summary>
public sealed class GetEventsQueryHandler(IApplicationDbContext context) : IRequestHandler<GetEventsQuery, List<EventDto>>
{
    /// <summary>
    /// Handles the query execution.
    /// </summary>
    /// <param name="request">The query request with optional date filters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of event DTOs matching the filter criteria, ordered by date ascending.</returns>
    public async Task<List<EventDto>> Handle(GetEventsQuery request, CancellationToken cancellationToken)
    {
        var query = context.Events.AsQueryable();

        // Apply start date filter (inclusive)
        if (request.StartDate.HasValue)
        {
            query = query.Where(e => e.Date >= request.StartDate.Value);
        }

        // Apply end date filter (inclusive)
        if (request.EndDate.HasValue)
        {
            query = query.Where(e => e.Date <= request.EndDate.Value);
        }

        // Project to DTO with AvailableSlots calculated in database query
        // This approach avoids N+1 queries and uses a single SQL query with aggregation
        var events = await query
            .OrderBy(e => e.Date)
            .Select(e => new EventDto
            {
                Id = e.Id,
                Title = e.Title,
                Date = e.Date,
                Location = e.Location,
                MaxCapacity = e.MaxCapacity,
                AvailableSlots = e.MaxCapacity - e.Registrations.Count(r => r.Status != RegistrationStatus.Cancelled)
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return events;
    }
}
