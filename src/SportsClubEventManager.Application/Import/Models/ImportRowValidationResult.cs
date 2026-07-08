using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Application.Import.Models;

/// <summary>
/// Result of validating and normalizing a single candidate row, produced by
/// <see cref="Common.Interfaces.IEventImportValidationService"/> for one entry of the batch
/// passed to it.
/// </summary>
public sealed class ImportRowValidationResult
{
    /// <summary>
    /// Gets the already-normalized candidate (trimmed, and title-cased if enabled).
    /// </summary>
    public required ImportEventItemDto NormalizedItem { get; init; }

    /// <summary>
    /// Gets a value indicating whether this row passed field-level validation and is not a duplicate.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Gets a value indicating whether this row's <c>(Title, Date)</c> key — exact date and
    /// time, title compared case-insensitively — matches another row in the same batch or an
    /// already-persisted event.
    /// </summary>
    public bool IsDuplicate { get; init; }

    /// <summary>
    /// Gets the combined list of field-level validation and duplicate-detection error messages
    /// for this row. Empty when <see cref="IsValid"/> is <see langword="true"/>.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = [];
}
