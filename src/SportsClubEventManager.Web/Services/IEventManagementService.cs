using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Web.Services;

/// <summary>
/// Defines the contract for event management operations (administrator only).
/// </summary>
public interface IEventManagementService
{
    /// <summary>
    /// Retrieves a paginated, filtered, and sorted list of events.
    /// </summary>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="fromDate">Optional start date filter.</param>
    /// <param name="toDate">Optional end date filter.</param>
    /// <param name="isUpcoming">Optional status filter (true = upcoming, false = past, null = all).</param>
    /// <param name="searchText">Optional search text for title or location.</param>
    /// <param name="sortBy">The field to sort by.</param>
    /// <param name="sortOrder">The sort order (asc or desc).</param>
    /// <returns>A paginated result containing event list data.</returns>
    Task<PagedResult<EventAdminListDto>> GetAllEventsAsync(
        int pageNumber = 1,
        int pageSize = 20,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        bool? isUpcoming = null,
        string? searchText = null,
        string sortBy = "Date",
        string sortOrder = "asc");

    /// <summary>
    /// Creates a new event.
    /// </summary>
    /// <param name="request">The event creation request.</param>
    /// <returns>The ID of the newly created event.</returns>
    Task<Guid> CreateEventAsync(CreateEventRequest request);

    /// <summary>
    /// Updates an existing event.
    /// </summary>
    /// <param name="eventId">The unique identifier of the event to update.</param>
    /// <param name="request">The update request containing new values.</param>
    /// <returns>The updated event details.</returns>
    Task<EventAdminListDto> UpdateEventAsync(Guid eventId, UpdateEventRequest request);

    /// <summary>
    /// Deletes an event and cancels all associated registrations.
    /// </summary>
    /// <param name="eventId">The unique identifier of the event to delete.</param>
    /// <returns>A response indicating the result of the deletion operation.</returns>
    Task<DeleteEventResponse> DeleteEventAsync(Guid eventId);
}
