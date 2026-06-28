namespace SportsClubEventManager.Shared.DTOs;

/// <summary>
/// Data transfer object for event information exposed via API.
/// </summary>
public sealed record EventDto
{
    /// <summary>
    /// Gets the unique identifier of the event.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the title of the event.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the date and time when the event takes place.
    /// </summary>
    public required DateTime Date { get; init; }

    /// <summary>
    /// Gets the location where the event takes place.
    /// </summary>
    public required string Location { get; init; }

    /// <summary>
    /// Gets the maximum capacity of the event.
    /// </summary>
    public required int MaxCapacity { get; init; }

    /// <summary>
    /// Gets the number of available slots remaining for the event.
    /// Calculated as MaxCapacity minus active registrations.
    /// </summary>
    public required int AvailableSlots { get; init; }
}
