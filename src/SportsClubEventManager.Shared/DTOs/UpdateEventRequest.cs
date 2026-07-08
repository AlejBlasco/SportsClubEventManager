namespace SportsClubEventManager.Shared.DTOs;

/// <summary>
/// Request model for updating an existing event.
/// </summary>
public class UpdateEventRequest
{
    /// <summary>
    /// Gets or sets the title of the event.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the event.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the event takes place.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Gets or sets the location where the event takes place.
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum capacity of the event.
    /// </summary>
    public int MaxCapacity { get; set; }

    /// <summary>
    /// Gets or sets the row version for optimistic concurrency control.
    /// </summary>
    public byte[]? RowVersion { get; set; }
}
