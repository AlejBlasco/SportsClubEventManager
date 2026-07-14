using MediatR;
using Microsoft.EntityFrameworkCore;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Domain.Enums;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Application.Events.Queries.GetEventsAdmin;

/// <summary>
/// Handler for retrieving a paginated, filtered list of events for administrative purposes.
/// </summary>
public class GetEventsAdminQueryHandler : IRequestHandler<GetEventsAdminQuery, PagedResult<EventAdminListDto>>
{
    private readonly IApplicationDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetEventsAdminQueryHandler"/> class.
    /// </summary>
    /// <param name="context">The application database context.</param>
    public GetEventsAdminQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Handles the query to retrieve a paginated list of events with applied filters and sorting.
    /// </summary>
    /// <param name="request">The query containing pagination, filter, and sort parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated result containing event list data and pagination metadata.</returns>
    public async Task<PagedResult<EventAdminListDto>> Handle(GetEventsAdminQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Events.AsQueryable();

        var now = DateTime.UtcNow;

        // Apply filters
        if (request.FromDate.HasValue)
        {
            query = query.Where(e => e.Date >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(e => e.Date <= request.ToDate.Value);
        }

        if (request.IsUpcoming.HasValue)
        {
            if (request.IsUpcoming.Value)
            {
                query = query.Where(e => e.Date >= now);
            }
            else
            {
                query = query.Where(e => e.Date < now);
            }
        }

        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            var searchLower = request.SearchText.ToLower();
            query = query.Where(e =>
                e.Title.ToLower().Contains(searchLower) ||
                e.Location.ToLower().Contains(searchLower));
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        query = request.SortBy.ToLower() switch
        {
            "title" => request.SortOrder.ToLower() == "desc"
                ? query.OrderByDescending(e => e.Title)
                : query.OrderBy(e => e.Title),
            "location" => request.SortOrder.ToLower() == "desc"
                ? query.OrderByDescending(e => e.Location)
                : query.OrderBy(e => e.Location),
            "maxcapacity" => request.SortOrder.ToLower() == "desc"
                ? query.OrderByDescending(e => e.MaxCapacity)
                : query.OrderBy(e => e.MaxCapacity),
            "createdat" => request.SortOrder.ToLower() == "desc"
                ? query.OrderByDescending(e => e.CreatedAt)
                : query.OrderBy(e => e.CreatedAt),
            _ => request.SortOrder.ToLower() == "desc"
                ? query.OrderByDescending(e => e.Date)
                : query.OrderBy(e => e.Date)
        };

        // Apply pagination and project to DTO
        var events = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Include(e => e.Registrations)
            .Select(e => new EventAdminListDto
            {
                Id = e.Id,
                Title = e.Title,
                Date = e.Date,
                Location = e.Location,
                Description = e.Description,
                MaxCapacity = e.MaxCapacity,
                CurrentRegistrations = e.Registrations.Count(r => r.Status != RegistrationStatus.Cancelled),
                IsPastEvent = e.Date < now,
                CreatedAt = e.CreatedAt,
                RowVersion = e.RowVersion
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<EventAdminListDto>
        {
            Items = events,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }
}
