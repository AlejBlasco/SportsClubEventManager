namespace SportsClubEventManager.Application.Import.Models;

/// <summary>
/// Internal parse-result model produced by <see cref="Common.Interfaces.ICsvEventImportParser"/>
/// for an entire uploaded CSV file.
/// </summary>
public sealed class CsvParseResult
{
    /// <summary>
    /// Gets or sets the column headers detected in the uploaded file.
    /// </summary>
    public IReadOnlyList<string> DetectedHeaders { get; init; } = [];

    /// <summary>
    /// Gets or sets the suggested mapping from detected headers to the standardized column names.
    /// </summary>
    public IReadOnlyDictionary<string, string> SuggestedMapping { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets the parsed rows. Empty when <see cref="FatalError"/> is set.
    /// </summary>
    public IReadOnlyList<ImportRowParseResult> Rows { get; init; } = [];

    /// <summary>
    /// Gets or sets a structural, file-level error (e.g. unreadable file, unsupported encoding,
    /// missing required header) that prevented parsing entirely.
    /// </summary>
    public string? FatalError { get; init; }
}
