using System.Globalization;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SportsClubEventManager.Application.Common.Constants;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Application.Import.Models;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Application.Import.Services;

/// <summary>
/// <see cref="IEventImportValidationService"/> implementation. Runs, once per batch: title/location
/// normalization, field-level validation (delegated to the existing <see cref="IValidator{T}"/>
/// for <see cref="ImportEventItemDto"/>), intra-batch duplicate detection, and duplicate detection
/// against already-persisted events via a single, date-range-bounded query.
/// </summary>
public sealed class EventImportValidationService : IEventImportValidationService
{
    private const string PersistedDuplicateErrorMessage = "An event with this title and date already exists";

    private readonly IApplicationDbContext _context;
    private readonly IValidator<ImportEventItemDto> _itemValidator;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EventImportValidationService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventImportValidationService"/> class.
    /// </summary>
    /// <param name="context">The application database context, used for the persisted-duplicate lookup.</param>
    /// <param name="itemValidator">The shared field-level validator for mapped event rows.</param>
    /// <param name="configuration">The application configuration, used to resolve import defaults.</param>
    /// <param name="logger">The logger for structured, content-free diagnostics.</param>
    public EventImportValidationService(
        IApplicationDbContext context,
        IValidator<ImportEventItemDto> itemValidator,
        IConfiguration configuration,
        ILogger<EventImportValidationService> logger)
    {
        _context = context;
        _itemValidator = itemValidator;
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ImportRowValidationResult>> ValidateAsync(
        IReadOnlyList<ImportEventItemDto> candidates,
        CancellationToken cancellationToken)
    {
        if (candidates.Count == 0)
        {
            return [];
        }

        var normalizeTitleCapitalization = _configuration.GetValue(
            ImportSettingsKeys.NormalizeTitleCapitalization,
            ImportSettingsKeys.NormalizeTitleCapitalizationFallback);

        var normalized = candidates
            .Select(candidate => Normalize(candidate, normalizeTitleCapitalization))
            .ToList();

        var intraBatchDuplicateOfRow = DetectIntraBatchDuplicates(normalized);
        var isPersistedDuplicate = await DetectPersistedDuplicatesAsync(normalized, cancellationToken);

        var results = new List<ImportRowValidationResult>(normalized.Count);

        for (var i = 0; i < normalized.Count; i++)
        {
            var fieldErrors = _itemValidator.Validate(normalized[i]).Errors.Select(e => e.ErrorMessage);
            var duplicateErrors = new List<string>();

            if (intraBatchDuplicateOfRow[i] is { } firstRowNumber)
            {
                duplicateErrors.Add($"Duplicate of row {firstRowNumber}");
            }

            if (isPersistedDuplicate[i])
            {
                duplicateErrors.Add(PersistedDuplicateErrorMessage);
            }

            var errors = fieldErrors.Concat(duplicateErrors).Distinct().ToList();

            results.Add(new ImportRowValidationResult
            {
                NormalizedItem = normalized[i],
                IsValid = errors.Count == 0,
                IsDuplicate = duplicateErrors.Count > 0,
                Errors = errors
            });
        }

        _logger.LogInformation(
            "Validated {TotalRows} import row(s): {IntraBatchDuplicates} intra-batch duplicate(s), {PersistedDuplicates} duplicate(s) against existing events.",
            normalized.Count,
            intraBatchDuplicateOfRow.Count(r => r is not null),
            isPersistedDuplicate.Count(d => d));

        return results;
    }

    /// <summary>
    /// Trims <see cref="ImportEventItemDto.Title"/>/<see cref="ImportEventItemDto.Location"/> and,
    /// when enabled, title-cases the title. Applied defensively even though the CSV parser already
    /// trims its output, since candidates may arrive directly from a client payload (confirm step).
    /// </summary>
    /// <param name="candidate">The candidate row to normalize.</param>
    /// <param name="normalizeTitleCapitalization">Whether to title-case the trimmed title.</param>
    /// <returns>A new, normalized candidate; the input is left unmodified.</returns>
    private static ImportEventItemDto Normalize(ImportEventItemDto candidate, bool normalizeTitleCapitalization)
    {
        var trimmedTitle = candidate.Title.Trim();
        var title = normalizeTitleCapitalization && trimmedTitle.Length > 0
            ? CultureInfo.InvariantCulture.TextInfo.ToTitleCase(trimmedTitle.ToLowerInvariant())
            : trimmedTitle;

        return new ImportEventItemDto
        {
            Title = title,
            Date = candidate.Date,
            Description = candidate.Description,
            Location = candidate.Location.Trim(),
            MaxCapacity = candidate.MaxCapacity
        };
    }

    /// <summary>
    /// Groups the normalized batch by <c>(Title.ToUpperInvariant(), Date)</c> — exact date and
    /// time — and flags every row after the first in a group as a duplicate of that first row.
    /// </summary>
    /// <param name="normalized">The already-normalized batch, in row order.</param>
    /// <returns>
    /// For each row, the 1-based row number of the earlier row it duplicates, or
    /// <see langword="null"/> when it is not an intra-batch duplicate.
    /// </returns>
    private static int?[] DetectIntraBatchDuplicates(IReadOnlyList<ImportEventItemDto> normalized)
    {
        var firstRowNumberByKey = new Dictionary<(string Title, DateTime Date), int>();
        var duplicateOfRow = new int?[normalized.Count];

        for (var i = 0; i < normalized.Count; i++)
        {
            var key = (normalized[i].Title.ToUpperInvariant(), normalized[i].Date);

            if (firstRowNumberByKey.TryGetValue(key, out var firstRowNumber))
            {
                duplicateOfRow[i] = firstRowNumber;
            }
            else
            {
                firstRowNumberByKey[key] = i + 1;
            }
        }

        return duplicateOfRow;
    }

    /// <summary>
    /// Detects rows whose <c>(Title, Date)</c> key matches an already-persisted event, via a
    /// single query bounded by the batch's date range (so the existing index on
    /// <c>Event.Date</c> can be used), comparing the exact key in memory afterwards.
    /// </summary>
    /// <param name="normalized">The already-normalized batch, in row order.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>For each row, whether it matches an already-persisted event.</returns>
    private async Task<bool[]> DetectPersistedDuplicatesAsync(
        IReadOnlyList<ImportEventItemDto> normalized,
        CancellationToken cancellationToken)
    {
        var minDate = normalized.Min(item => item.Date).Date;
        var maxDateExclusive = normalized.Max(item => item.Date).Date.AddDays(1);

        var existingEvents = await _context.Events
            .Where(e => e.Date >= minDate && e.Date < maxDateExclusive)
            .Select(e => new { e.Title, e.Date })
            .ToListAsync(cancellationToken);

        var existingKeys = existingEvents
            .Select(e => (e.Title.ToUpperInvariant(), e.Date))
            .ToHashSet();

        var isDuplicate = new bool[normalized.Count];

        for (var i = 0; i < normalized.Count; i++)
        {
            var key = (normalized[i].Title.ToUpperInvariant(), normalized[i].Date);
            isDuplicate[i] = existingKeys.Contains(key);
        }

        return isDuplicate;
    }
}
