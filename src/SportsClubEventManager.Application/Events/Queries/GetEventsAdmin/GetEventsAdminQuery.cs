using MediatR;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Application.Events.Queries.GetEventsAdmin;

/// <summary>
/// Query to retrieve a paginated, filtered list of events for administrative purposes.
/// </summary>
public class GetEventsAdminQuery : IRequest<PagedResult<EventAdminListDto>>
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
    /// Gets or sets the start date filter. If specified, only events on or after this date are returned.
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Gets or sets the end date filter. If specified, only events on or before this date are returned.
    /// </summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Gets or sets the status filter. If true, only upcoming events are returned. If false, only past events. If null, all events.
    /// </summary>
    public bool? IsUpcoming { get; set; }

    /// <summary>
    /// Gets or sets the search text. If specified, searches for events whose title or location contains this text.
    /// </summary>
    public string? SearchText { get; set; }

    /// <summary>
    /// Gets or sets the field to sort by. Defaults to "Date".
    /// </summary>
    public string SortBy { get; set; } = "Date";

    /// <summary>
    /// Gets or sets the sort order. Defaults to "asc" (ascending).
    /// </summary>
    public string SortOrder { get; set; } = "asc";
}
