using MediatR;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Application.Import.Commands.ParseCsvFile;

/// <summary>
/// Command to parse an uploaded CSV file into a preview of mapped event rows.
/// Performs no database writes; used for the dry-run preview step of the CSV import flow.
/// </summary>
public sealed class ParseCsvFileCommand : IRequest<CsvImportPreviewResponse>
{
    /// <summary>
    /// Gets or sets the uploaded CSV file content.
    /// </summary>
    public required Stream FileStream { get; init; }

    /// <summary>
    /// Gets or sets the original uploaded file name (used for structural checks and logging).
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// Gets or sets an optional column mapping from headers found in the file to the
    /// standardized column names, used when the file's headers do not match exactly.
    /// </summary>
    public IReadOnlyDictionary<string, string>? ColumnMapping { get; init; }

    /// <summary>
    /// Gets or sets an optional override for the default maximum capacity applied to every
    /// row. Falls back to the configured "ImportSettings:DefaultMaxCapacity" when not supplied.
    /// </summary>
    public int? DefaultMaxCapacity { get; init; }
}
