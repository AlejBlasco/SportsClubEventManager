using System.Net.Http.Json;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Web.Services;

/// <summary>
/// Service for event management operations, communicating with the API.
/// </summary>
public class EventManagementService : IEventManagementService
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventManagementService"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client for API communication.</param>
    public EventManagementService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

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
    public async Task<PagedResult<EventAdminListDto>> GetAllEventsAsync(
        int pageNumber = 1,
        int pageSize = 20,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        bool? isUpcoming = null,
        string? searchText = null,
        string sortBy = "Date",
        string sortOrder = "asc")
    {
        var queryParams = new List<string>
        {
            $"pageNumber={pageNumber}",
            $"pageSize={pageSize}",
            $"sortBy={sortBy}",
            $"sortOrder={sortOrder}"
        };

        if (fromDate.HasValue)
        {
            queryParams.Add($"fromDate={fromDate.Value:yyyy-MM-ddTHH:mm:ss}");
        }

        if (toDate.HasValue)
        {
            queryParams.Add($"toDate={toDate.Value:yyyy-MM-ddTHH:mm:ss}");
        }

        if (isUpcoming.HasValue)
        {
            queryParams.Add($"isUpcoming={isUpcoming.Value}");
        }

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            queryParams.Add($"searchText={Uri.EscapeDataString(searchText)}");
        }

        var url = $"api/admin/events?{string.Join("&", queryParams)}";
        var response = await _httpClient.GetFromJsonAsync<PagedResult<EventAdminListDto>>(url);

        return response ?? new PagedResult<EventAdminListDto>();
    }

    /// <summary>
    /// Creates a new event.
    /// </summary>
    /// <param name="request">The event creation request.</param>
    /// <returns>The ID of the newly created event.</returns>
    public async Task<Guid> CreateEventAsync(CreateEventRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/admin/events", request);
        response.EnsureSuccessStatusCode();

        var eventId = await response.Content.ReadFromJsonAsync<Guid>();
        return eventId;
    }

    /// <summary>
    /// Updates an existing event.
    /// </summary>
    /// <param name="eventId">The unique identifier of the event to update.</param>
    /// <param name="request">The update request containing new values.</param>
    /// <returns>The updated event details.</returns>
    public async Task<EventAdminListDto> UpdateEventAsync(Guid eventId, UpdateEventRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/admin/events/{eventId}", request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<EventAdminListDto>();
        return result ?? throw new InvalidOperationException("Failed to update event.");
    }

    /// <summary>
    /// Deletes an event and cancels all associated registrations.
    /// </summary>
    /// <param name="eventId">The unique identifier of the event to delete.</param>
    /// <returns>A response indicating the result of the deletion operation.</returns>
    public async Task<DeleteEventResponse> DeleteEventAsync(Guid eventId)
    {
        var response = await _httpClient.DeleteAsync($"api/admin/events/{eventId}");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<DeleteEventResponse>();
        return result ?? throw new InvalidOperationException("Failed to delete event.");
    }
}
