using System.Net.Http.Json;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Web.Services;

/// <summary>
/// Implements event retrieval from the API using HttpClient.
/// </summary>
/// <param name="httpClient">HTTP client configured with the API base URL.</param>
public sealed class EventService(HttpClient httpClient) : IEventService
{
    /// <summary>
    /// Retrieves all events from the API.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A list of events, or an empty list if no events are available.</returns>
    /// <exception cref="HttpRequestException">Thrown when the API request fails.</exception>
    public async Task<List<EventDto>> GetEventsAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync("api/v1/events", cancellationToken);
        response.EnsureSuccessStatusCode();

        var events = await response.Content.ReadFromJsonAsync<List<EventDto>>(cancellationToken);
        return events ?? [];
    }

    /// <summary>
    /// Retrieves a specific event by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the event to retrieve.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The event details if found; otherwise, null if the event does not exist.</returns>
    /// <exception cref="HttpRequestException">Thrown when the API request fails with a non-404 error.</exception>
    public async Task<EventDetailDto?> GetEventByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync($"api/v1/events/{id}", cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var eventDetail = await response.Content.ReadFromJsonAsync<EventDetailDto>(cancellationToken);
        return eventDetail;
    }
}
