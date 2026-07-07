using MediatR;
using SportsClubEventManager.Domain.Enums;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Application.Registrations.Queries.GetRegistrationsAdmin;

/// <summary>
/// Query to retrieve paginated registrations for administrators.
/// </summary>
public sealed class GetRegistrationsAdminQuery : IRequest<PagedResult<RegistrationListDto>>
{
    /// <summary>
    /// Gets or sets the page number.
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Gets or sets an optional event filter.
    /// </summary>
    public Guid? EventId { get; set; }

    /// <summary>
    /// Gets or sets an optional user filter.
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Gets or sets an optional status filter.
    /// </summary>
    public RegistrationStatus? Status { get; set; }

    /// <summary>
    /// Gets or sets an optional event date lower bound.
    /// </summary>
    public DateTime? EventDateFrom { get; set; }

    /// <summary>
    /// Gets or sets an optional event date upper bound.
    /// </summary>
    public DateTime? EventDateTo { get; set; }

    /// <summary>
    /// Gets or sets optional search text for event or user fields.
    /// </summary>
    public string? SearchText { get; set; }

    /// <summary>
    /// Gets or sets the field used for sorting.
    /// </summary>
    public string SortBy { get; set; } = "RegistrationDate";

    /// <summary>
    /// Gets or sets the sort order (asc or desc).
    /// </summary>
    public string SortOrder { get; set; } = "desc";
}
