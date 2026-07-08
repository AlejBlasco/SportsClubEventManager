namespace SportsClubEventManager.Shared.DTOs;

/// <summary>
/// Result returned by the CSV import confirm endpoint, summarizing the outcome of the
/// all-or-nothing bulk insert.
/// </summary>
public sealed class CsvImportResultDto
{
    /// <summary>
    /// Gets or sets the number of events successfully imported.
    /// </summary>
    public int ImportedCount { get; set; }

    /// <summary>
    /// Gets or sets the number of rows that failed re-validation and were not imported.
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// Gets or sets the rows that failed re-validation, with their associated error messages.
    /// Empty when the import succeeded.
    /// </summary>
    public IReadOnlyList<CsvImportRowDto> FailedRows { get; set; } = [];
}
