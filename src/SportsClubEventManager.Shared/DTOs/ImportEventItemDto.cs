namespace SportsClubEventManager.Shared.DTOs;

/// <summary>
/// Represents a single admin-approved event row, already mapped from the CSV source
/// columns to the five writable <c>Event</c> fields. This is the shape submitted to
/// the confirm-import endpoint, not the raw CSV row.
/// </summary>
public sealed class ImportEventItemDto
{
    /// <summary>
    /// Gets or sets the event title (mapped from the CSV "NOMBRE TIRADA" column).
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event date and time (combined from the CSV "DÍA" and "HORA" columns).
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Gets or sets the event description (composed from the CSV "MODAL.", "CAMPO" and "CAT" columns).
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the event location (mapped from the CSV "LUGAR" column).
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum capacity of the event. The CSV has no source column for this
    /// value, so it defaults to a configured value and can be edited per row before confirming.
    /// </summary>
    public int MaxCapacity { get; set; }
}
