using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Web.Services;

/// <summary>
/// Defines the contract for retrieving event information from the API.
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
}
