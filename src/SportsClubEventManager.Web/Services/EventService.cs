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
}
