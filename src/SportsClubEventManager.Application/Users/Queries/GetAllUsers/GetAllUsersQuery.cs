using MediatR;
using SportsClubEventManager.Domain.Enums;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Application.Users.Queries.GetAllUsers;

/// <summary>
/// Query to retrieve a paginated, filtered, and sorted list of users for administrative purposes.
/// </summary>
public class GetAllUsersQuery : IRequest<PagedResult<UserListDto>>
{
    /// <summary>
    /// Gets or sets the current page number (1-based).
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Gets or sets the number of items per page.
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Gets or sets the role filter. If specified, only users with this role are returned.
    /// </summary>
    public Role? RoleFilter { get; set; }

    /// <summary>
    /// Gets or sets the status filter. If specified, only users with this status are returned.
    /// </summary>
    public bool? IsActiveFilter { get; set; }

    /// <summary>
    /// Gets or sets the search text. If specified, searches for users whose name or email contains this text.
    /// </summary>
    public string? SearchText { get; set; }

    /// <summary>
    /// Gets or sets the field to sort by. Defaults to "Name".
    /// </summary>
    public string SortBy { get; set; } = "Name";

    /// <summary>
    /// Gets or sets the sort order. Defaults to "asc" (ascending).
    /// </summary>
    public string SortOrder { get; set; } = "asc";
}
