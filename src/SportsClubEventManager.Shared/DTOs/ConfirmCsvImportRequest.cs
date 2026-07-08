namespace SportsClubEventManager.Shared.DTOs;

/// <summary>
/// Request payload for confirming a CSV import. Contains the admin-approved, already-mapped
/// event rows from the preview step (not a raw file re-upload).
/// </summary>
public sealed class ConfirmCsvImportRequest
{
    /// <summary>
    /// Gets or sets the admin-approved list of event rows to import.
    /// </summary>
    public IReadOnlyList<ImportEventItemDto> Events { get; set; } = [];
}
