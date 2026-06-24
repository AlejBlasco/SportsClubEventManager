using MediatR;
using Microsoft.EntityFrameworkCore;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Domain.Enums;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Application.Events.Queries.GetEventById;

/// <summary>
/// Handles the GetEventByIdQuery by retrieving a specific event from the database with calculated registration details.
/// </summary>
public sealed class GetEventByIdQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetEventByIdQuery, EventDetailDto?>
{
    /// <summary>
    /// Handles the query execution.
    /// </summary>
    /// <param name="request">The query request containing the event identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The event detail DTO if found; otherwise, null.</returns>
    public async Task<EventDetailDto?> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
    {
        // Retrieve the event with registrations using a single query to prevent N+1 problem
        var eventDetail = await context.Events
            .Where(e => e.Id == request.EventId)
            .Select(e => new EventDetailDto
            {
                Id = e.Id,
                Title = e.Title,
                Description = e.Description,
                Date = e.Date,
                Location = e.Location,
                MaxCapacity = e.MaxCapacity,
                CurrentRegistrations = e.Registrations.Count(r => r.Status != RegistrationStatus.Cancelled),
                AvailableSlots = e.MaxCapacity - e.Registrations.Count(r => r.Status != RegistrationStatus.Cancelled),
                IsFullyBooked = e.Registrations.Count(r => r.Status != RegistrationStatus.Cancelled) >= e.MaxCapacity
            })
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        return eventDetail;
    }
}
