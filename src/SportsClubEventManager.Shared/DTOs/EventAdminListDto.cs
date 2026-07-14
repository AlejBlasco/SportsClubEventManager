namespace SportsClubEventManager.Shared.DTOs;

/// <summary>
/// Data transfer object for event information in administrative list view.
/// Contains additional fields not present in the public event list.
/// </summary>
public class EventAdminListDto
{
    /// <summary>
    /// Gets or sets the unique identifier of the event.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the title of the event.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the event takes place.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Gets or sets the location where the event takes place.
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the event.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the maximum capacity of the event.
    /// </summary>
    public int MaxCapacity { get; set; }

    /// <summary>
    /// Gets or sets the current number of active registrations.
    /// Only counts registrations that are not cancelled.
    /// </summary>
    public int CurrentRegistrations { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the event date is in the past.
    /// </summary>
    public bool IsPastEvent { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the event was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the row version for optimistic concurrency control.
    /// </summary>
    public byte[]? RowVersion { get; set; }
}
