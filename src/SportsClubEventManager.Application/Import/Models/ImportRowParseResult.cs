namespace SportsClubEventManager.Application.Import.Models;

/// <summary>
/// Internal parse-result model for a single CSV row, produced by <see cref="Common.Interfaces.ICsvEventImportParser"/>.
/// Contains both the mapped <c>Event</c> fields and the raw source values the mapping was derived from.
/// </summary>
public sealed class ImportRowParseResult
{
    /// <summary>
    /// Gets or sets the 1-based row number within the file (excluding the header row).
    /// </summary>
    public int RowNumber { get; init; }

    /// <summary>
    /// Gets or sets the mapped event title, sourced from "NOMBRE TIRADA".
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Gets or sets the mapped event date and time, combined from "DÍA" and "HORA".
    /// Null when the source values could not be parsed.
    /// </summary>
    public DateTime? Date { get; init; }

    /// <summary>
    /// Gets or sets the mapped, composite event description, built from "MODAL.", "CAMPO" and "CAT".
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets or sets the mapped event location, sourced from "LUGAR".
    /// </summary>
    public string? Location { get; init; }

    /// <summary>
    /// Gets or sets the mapped event maximum capacity, applied from the configured default.
    /// </summary>
    public int? MaxCapacity { get; init; }

    /// <summary>
    /// Gets or sets the raw "DÍA" (day) source column value.
    /// </summary>
    public string? SourceDay { get; init; }

    /// <summary>
    /// Gets or sets the raw "HORA" (time) source column value.
    /// </summary>
    public string? SourceTime { get; init; }

    /// <summary>
    /// Gets or sets the raw "MODAL." (modality) source column value.
    /// </summary>
    public string? SourceModality { get; init; }

    /// <summary>
    /// Gets or sets the raw "CAMPO" (field/range) source column value.
    /// </summary>
    public string? SourceField { get; init; }

    /// <summary>
    /// Gets or sets the raw "CAT" (category) source column value.
    /// </summary>
    public string? SourceCategory { get; init; }

    /// <summary>
    /// Gets or sets the list of parsing errors detected for this row (e.g. unparseable date/time).
    /// Field-level validation errors (required, max length, etc.) are added on top of these
    /// by <see cref="Application.Import.Commands.ParseCsvFile.ParseCsvFileCommandHandler"/>.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = [];
}
