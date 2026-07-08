using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Web.Services;

/// <summary>
/// Defines the contract for CSV event import operations (administrator only).
/// </summary>
public interface IImportManagementService
{
    /// <summary>
    /// Downloads the standardized CSV import template as UTF-8 text.
    /// </summary>
    /// <returns>The template file content.</returns>
    Task<string> DownloadTemplateAsync();

    /// <summary>
    /// Uploads a CSV file for a dry-run preview: parses and maps the rows without writing to the database.
    /// </summary>
    /// <param name="fileContent">The raw uploaded file content.</param>
    /// <param name="fileName">The uploaded file name.</param>
    /// <param name="columnMapping">Optional column remapping entries, used when headers do not match exactly.</param>
    /// <param name="defaultMaxCapacity">Optional override for the default maximum capacity applied to every row.</param>
    /// <returns>The preview response, including per-row validation results.</returns>
    Task<CsvImportPreviewResponse> PreviewCsvAsync(
        byte[] fileContent,
        string fileName,
        IReadOnlyList<ImportColumnMappingDto>? columnMapping = null,
        int? defaultMaxCapacity = null);

    /// <summary>
    /// Confirms a CSV import, inserting all admin-approved rows in a single, all-or-nothing transaction.
    /// </summary>
    /// <param name="events">The admin-approved, already-mapped event rows.</param>
    /// <returns>The import result.</returns>
    Task<CsvImportResultDto> ConfirmImportAsync(IReadOnlyList<ImportEventItemDto> events);
}
