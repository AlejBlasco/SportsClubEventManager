namespace SportsClubEventManager.Shared.DTOs;

/// <summary>
/// Response returned by the CSV import preview (dry-run) endpoint. Contains the parsed,
/// mapped rows and per-row validation results. No database writes occur for this response.
/// </summary>
public sealed class CsvImportPreviewResponse
{
    /// <summary>
    /// Gets or sets the column headers detected in the uploaded file.
    /// </summary>
    public IReadOnlyList<string> DetectedHeaders { get; set; } = [];

    /// <summary>
    /// Gets or sets the suggested mapping from detected headers to the standardized column names,
    /// useful for pre-filling the column remapping UI when headers are close but not an exact match.
    /// </summary>
    public IReadOnlyDictionary<string, string> SuggestedMapping { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets the parsed and mapped rows, including per-row validation errors.
    /// </summary>
    public IReadOnlyList<CsvImportRowDto> Rows { get; set; } = [];

    /// <summary>
    /// Gets or sets the total number of data rows found in the file.
    /// </summary>
    public int TotalRows { get; set; }

    /// <summary>
    /// Gets or sets the number of rows that passed field-level validation.
    /// </summary>
    public int ValidRowCount { get; set; }

    /// <summary>
    /// Gets or sets the number of rows that failed field-level validation.
    /// </summary>
    public int InvalidRowCount { get; set; }

    /// <summary>
    /// Gets or sets a structural, file-level error (e.g. unreadable file, missing required
    /// header) that prevented parsing entirely. When set, <see cref="Rows"/> is empty.
    /// </summary>
    public string? FatalError { get; set; }
}
