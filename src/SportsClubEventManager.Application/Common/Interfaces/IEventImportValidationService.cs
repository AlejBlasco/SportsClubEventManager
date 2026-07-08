using SportsClubEventManager.Application.Import.Models;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Application.Common.Interfaces;

/// <summary>
/// Validates and normalizes a batch of candidate event rows before they are previewed or
/// persisted. Centralizes the three responsibilities that used to be duplicated, per row,
/// across <c>ParseCsvFileCommandHandler</c> and <c>BulkCreateEventsCommandHandler</c>: title/location
/// normalization, field-level validation, and duplicate detection (both intra-batch and against
/// already-persisted events).
/// </summary>
public interface IEventImportValidationService
{
    /// <summary>
    /// Validates and normalizes every candidate in the batch, in a single pass.
    /// </summary>
    /// <param name="candidates">The candidate event rows to validate, in file/row order.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// One <see cref="ImportRowValidationResult"/> per candidate, in the same order as
    /// <paramref name="candidates"/>.
    /// </returns>
    Task<IReadOnlyList<ImportRowValidationResult>> ValidateAsync(
        IReadOnlyList<ImportEventItemDto> candidates,
        CancellationToken cancellationToken);
}
