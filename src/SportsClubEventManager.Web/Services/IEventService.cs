using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Web.Services;

/// <summary>
/// Defines the contract for retrieving event information and managing registrations via the API.
/// </summary>
public interface IEventService
{
    /// <summary>
    /// Retrieves all events from the API.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A list of events, or an empty list if no events are available.</returns>
    Task<List<EventDto>> GetEventsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific event by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the event to retrieve.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The event details if found; otherwise, null.</returns>
    Task<EventDetailDto?> GetEventByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a user for a specific event.
    /// </summary>
    /// <param name="eventId">The unique identifier of the event to register for.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>Registration details if successful; otherwise, null if registration failed (event full, duplicate, or not found).</returns>
    Task<RegistrationCreatedDto?> RegisterForEventAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a registration owned by the authenticated user.
    /// </summary>
    /// <param name="registrationId">The unique identifier of the registration.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>True if cancellation was successful; false if registration was not found or event does not exist.</returns>
    Task<bool> CancelRegistrationAsync(Guid registrationId, CancellationToken cancellationToken = default);
}
