namespace SportsClubEventManager.Shared.DTOs;

/// <summary>
/// Represents one parsed row of a CSV import preview, including both the mapped
/// <c>Event</c> fields (editable by the admin before confirming) and the raw source
/// column values (read-only, shown for verification purposes).
/// </summary>
public sealed class CsvImportRowDto
{
    /// <summary>
    /// Gets or sets the 1-based row number within the uploaded file (excluding the header row).
    /// </summary>
    public int RowNumber { get; set; }

    /// <summary>
    /// Gets or sets the mapped event title. Editable by the admin in the preview grid.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the mapped event date and time. Null when "DÍA"/"HORA" could not be parsed.
    /// </summary>
    public DateTime? Date { get; set; }

    /// <summary>
    /// Gets or sets the mapped, composite event description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the mapped event location.
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the mapped event maximum capacity. Defaults to the configured value
    /// and is editable per row by the admin before confirming, since the CSV has no source column for it.
    /// </summary>
    public int? MaxCapacity { get; set; }

    /// <summary>
    /// Gets or sets the raw "DÍA" (day) column value, echoed back for verification.
    /// </summary>
    public string? SourceDay { get; set; }

    /// <summary>
    /// Gets or sets the raw "HORA" (time) column value, echoed back for verification.
    /// </summary>
    public string? SourceTime { get; set; }

    /// <summary>
    /// Gets or sets the raw "MODAL." (modality) column value, echoed back for verification.
    /// </summary>
    public string? SourceModality { get; set; }

    /// <summary>
    /// Gets or sets the raw "CAMPO" (field/range) column value, echoed back for verification.
    /// </summary>
    public string? SourceField { get; set; }

    /// <summary>
    /// Gets or sets the raw "CAT" (category) column value, echoed back for verification.
    /// </summary>
    public string? SourceCategory { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this row passed field-level validation.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this row's <c>(Title, Date)</c> key — exact date
    /// and time, title compared case-insensitively — matches another row in the same file or an
    /// already-persisted event. A duplicate row is always also <see cref="IsValid"/> <c>false</c>.
    /// </summary>
    public bool IsDuplicate { get; set; }

    /// <summary>
    /// Gets or sets the list of validation/parsing error messages for this row, if any.
    /// </summary>
    public IReadOnlyList<string> Errors { get; set; } = [];
}
