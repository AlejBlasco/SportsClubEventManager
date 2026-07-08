namespace SportsClubEventManager.Shared.DTOs;

/// <summary>
/// Represents a single column remapping entry, associating a column header actually found
/// in the uploaded CSV file with one of the standardized column names
/// ("DÍA", "MODAL.", "NOMBRE TIRADA", "HORA", "CAMPO", "LUGAR", "CAT").
/// Used when the admin's file headers do not exactly match the standardized schema.
/// </summary>
public sealed class ImportColumnMappingDto
{
    /// <summary>
    /// Gets or sets the column header as it appears in the uploaded file.
    /// </summary>
    public string SourceColumn { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the standardized column name it should be treated as.
    /// </summary>
    public string TargetColumn { get; set; } = string.Empty;
}
