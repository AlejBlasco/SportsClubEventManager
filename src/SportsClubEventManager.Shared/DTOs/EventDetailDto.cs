namespace SportsClubEventManager.Shared.DTOs;

/// <summary>
/// Data transfer object for detailed event information exposed via API.
/// Contains additional fields not present in the event list view.
/// </summary>
public sealed record EventDetailDto
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
    /// Gets the description of the event.
    /// </summary>
    public string? Description { get; init; }

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
    /// Gets the current number of active registrations.
    /// Only counts registrations that are not cancelled.
    /// </summary>
    public required int CurrentRegistrations { get; init; }

    /// <summary>
    /// Gets the number of available slots remaining for the event.
    /// Calculated as MaxCapacity minus CurrentRegistrations.
    /// </summary>
    public required int AvailableSlots { get; init; }

    /// <summary>
    /// Gets a value indicating whether the event has reached its maximum capacity.
    /// </summary>
    public required bool IsFullyBooked { get; init; }
}
