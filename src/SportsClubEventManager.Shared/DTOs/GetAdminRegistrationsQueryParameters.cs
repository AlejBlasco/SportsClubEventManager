using SportsClubEventManager.Domain.Enums;

namespace SportsClubEventManager.Shared.DTOs;

/// <summary>
/// Query parameters for the administrator registration list endpoint.
/// </summary>
public sealed class GetAdminRegistrationsQueryParameters
{
    /// <summary>
    /// Gets or sets the page number (1-based).
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Gets or sets an optional filter by event identifier.
    /// </summary>
    public Guid? EventId { get; set; }

    /// <summary>
    /// Gets or sets an optional filter by user identifier.
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Gets or sets an optional filter by registration status.
    /// </summary>
    public RegistrationStatus? Status { get; set; }

    /// <summary>
    /// Gets or sets an optional start date filter for event date.
    /// </summary>
    public DateTime? EventDateFrom { get; set; }

    /// <summary>
    /// Gets or sets an optional end date filter for event date.
    /// </summary>
    public DateTime? EventDateTo { get; set; }

    /// <summary>
    /// Gets or sets optional search text for event title, user name, or user email.
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
