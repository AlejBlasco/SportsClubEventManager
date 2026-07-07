using MediatR;
using Microsoft.EntityFrameworkCore;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Application.Registrations.Queries.GetRegistrationsAdmin;

/// <summary>
/// Handler for retrieving paginated registrations for administrators.
/// </summary>
public sealed class GetRegistrationsAdminQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetRegistrationsAdminQuery, PagedResult<RegistrationListDto>>
{
    /// <summary>
    /// Handles the query and returns matching registration rows.
    /// </summary>
    /// <param name="request">The request with filters and sorting.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paged registration list.</returns>
    public async Task<PagedResult<RegistrationListDto>> Handle(GetRegistrationsAdminQuery request, CancellationToken cancellationToken)
    {
        var query = context.Registrations
            .AsNoTracking()
            .Include(r => r.Event)
            .Include(r => r.User)
            .AsQueryable();

        if (request.EventId.HasValue)
        {
            query = query.Where(r => r.EventId == request.EventId.Value);
        }

        if (request.UserId.HasValue)
        {
            query = query.Where(r => r.UserId == request.UserId.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(r => r.Status == request.Status.Value);
        }

        if (request.EventDateFrom.HasValue)
        {
            query = query.Where(r => r.Event.Date >= request.EventDateFrom.Value);
        }

        if (request.EventDateTo.HasValue)
        {
            query = query.Where(r => r.Event.Date <= request.EventDateTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            var searchText = request.SearchText.Trim().ToLower();
            query = query.Where(r =>
                r.Event.Title.ToLower().Contains(searchText) ||
                r.User.Name.ToLower().Contains(searchText) ||
                r.User.Email.ToLower().Contains(searchText));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        query = (request.SortBy.ToLowerInvariant(), request.SortOrder.ToLowerInvariant()) switch
        {
            ("eventtitle", "asc") => query.OrderBy(r => r.Event.Title),
            ("eventtitle", "desc") => query.OrderByDescending(r => r.Event.Title),
            ("eventdate", "asc") => query.OrderBy(r => r.Event.Date),
            ("eventdate", "desc") => query.OrderByDescending(r => r.Event.Date),
            ("username", "asc") => query.OrderBy(r => r.User.Name),
            ("username", "desc") => query.OrderByDescending(r => r.User.Name),
            ("status", "asc") => query.OrderBy(r => r.Status),
            ("status", "desc") => query.OrderByDescending(r => r.Status),
            ("registrationdate", "asc") => query.OrderBy(r => r.RegistrationDate),
            _ => query.OrderByDescending(r => r.RegistrationDate)
        };

        var now = DateTime.UtcNow;
        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
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
                CanBeCancelledByUser = r.Status == Domain.Enums.RegistrationStatus.Registered && r.Event.Date >= now
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<RegistrationListDto>
        {
            Items = items,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }
}
