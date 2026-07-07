using MediatR;
using Microsoft.EntityFrameworkCore;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Domain.Enums;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Application.Registrations.Queries.GetUserRegistrations;

/// <summary>
/// Handler for retrieving user registrations.
/// </summary>
public sealed class GetUserRegistrationsQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetUserRegistrationsQuery, List<RegistrationListDto>>
{
    /// <summary>
    /// Handles the query and returns matching registrations.
    /// </summary>
    /// <param name="request">The query request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matching registrations for the user.</returns>
    public async Task<List<RegistrationListDto>> Handle(GetUserRegistrationsQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var query = context.Registrations
            .AsNoTracking()
            .Include(r => r.Event)
            .Include(r => r.User)
            .Where(r => r.UserId == request.UserId);

        if (request.OnlyActive)
        {
            query = query.Where(r => r.Status == RegistrationStatus.Registered && r.Event.Date >= now);
        }

        return await query
            .OrderBy(r => r.Event.Date)
            .Select(r => new RegistrationListDto
            {
                RegistrationId = r.Id,
                EventId = r.EventId,
                EventTitle = r.Event.Title,
                EventDate = r.Event.Date,
                EventLocation = r.Event.Location,
                UserId = r.UserId,
                UserName = r.User.Name,
                UserEmail = r.User.Email,
                RegistrationDate = r.RegistrationDate,
                Status = r.Status,
                CanBeCancelledByUser = r.Status == RegistrationStatus.Registered && r.Event.Date >= now
            })
            .ToListAsync(cancellationToken);
    }
}
