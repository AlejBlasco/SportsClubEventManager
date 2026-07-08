using System.Net.Http.Json;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Web.Services;

/// <summary>
/// Implements event retrieval and registration management from the API using HttpClient.
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

    /// <summary>
    /// Registers a user for a specific event.
    /// </summary>
    /// <param name="eventId">The unique identifier of the event to register for.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>Registration details if successful; otherwise, null if registration failed (event full, duplicate, or not found).</returns>
    /// <exception cref="HttpRequestException">Thrown when the API request fails with an unexpected error.</exception>
    public async Task<RegistrationCreatedDto?> RegisterForEventAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsync($"api/v1/events/{eventId}/register", content: null, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound ||
            response.StatusCode == System.Net.HttpStatusCode.Conflict ||
            response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var registrationCreated = await response.Content.ReadFromJsonAsync<RegistrationCreatedDto>(cancellationToken);
        return registrationCreated;
    }

    /// <summary>
    /// Cancels a registration owned by the authenticated user.
    /// </summary>
    /// <param name="registrationId">The unique identifier of the registration.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>True if cancellation was successful; false if registration was not found or event does not exist.</returns>
    /// <exception cref="HttpRequestException">Thrown when the API request fails with an unexpected error.</exception>
    public async Task<bool> CancelRegistrationAsync(Guid registrationId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync($"api/v1/registrations/{registrationId}", cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound ||
            response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();

        return response.StatusCode == System.Net.HttpStatusCode.NoContent;
    }
}
