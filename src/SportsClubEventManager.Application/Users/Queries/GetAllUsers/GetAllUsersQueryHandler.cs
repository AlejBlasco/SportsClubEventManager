using MediatR;
using Microsoft.EntityFrameworkCore;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Application.Users.Queries.GetAllUsers;

/// <summary>
/// Handler for retrieving a paginated, filtered list of users.
/// </summary>
public class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, PagedResult<UserListDto>>
{
    private readonly IApplicationDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetAllUsersQueryHandler"/> class.
    /// </summary>
    /// <param name="context">The application database context.</param>
    public GetAllUsersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Handles the query to retrieve a paginated list of users with applied filters and sorting.
    /// </summary>
    /// <param name="request">The query containing pagination, filter, and sort parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated result containing user list data and pagination metadata.</returns>
    public async Task<PagedResult<UserListDto>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Users.AsQueryable();

        // Apply filters
        if (request.RoleFilter.HasValue)
        {
            query = query.Where(u => u.Role == request.RoleFilter.Value);
        }

        if (request.IsActiveFilter.HasValue)
        {
            query = query.Where(u => u.IsActive == request.IsActiveFilter.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            var searchLower = request.SearchText.ToLower();
            query = query.Where(u =>
                u.Name.ToLower().Contains(searchLower) ||
                u.Email.ToLower().Contains(searchLower));
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        query = request.SortBy.ToLower() switch
        {
            "email" => request.SortOrder.ToLower() == "desc"
                ? query.OrderByDescending(u => u.Email)
                : query.OrderBy(u => u.Email),
            "role" => request.SortOrder.ToLower() == "desc"
                ? query.OrderByDescending(u => u.Role)
                : query.OrderBy(u => u.Role),
            "isactive" => request.SortOrder.ToLower() == "desc"
                ? query.OrderByDescending(u => u.IsActive)
                : query.OrderBy(u => u.IsActive),
            "lastloginat" => request.SortOrder.ToLower() == "desc"
                ? query.OrderByDescending(u => u.LastLoginAt)
                : query.OrderBy(u => u.LastLoginAt),
            "createdat" => request.SortOrder.ToLower() == "desc"
                ? query.OrderByDescending(u => u.CreatedAt)
                : query.OrderBy(u => u.CreatedAt),
            _ => request.SortOrder.ToLower() == "desc"
                ? query.OrderByDescending(u => u.Name)
                : query.OrderBy(u => u.Name)
        };

        // Apply pagination
        var users = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => new UserListDto
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                Role = u.Role,
                IsActive = u.IsActive,
                LastLoginAt = u.LastLoginAt,
                ProviderName = u.ProviderName,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<UserListDto>
        {
            Items = users,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }
}
