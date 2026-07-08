using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Application.Import.Models;
using SportsClubEventManager.Domain.Entities;
using SportsClubEventManager.Domain.Enums;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Application.Import.Commands.BulkCreateEvents;

/// <summary>
/// Handler for confirming a CSV import. Re-validates the whole batch as defense-in-depth against
/// a stale or tampered client payload, then inserts all events in a single, all-or-nothing
/// database transaction and writes one summary <c>AuditLog</c> entry.
/// </summary>
public sealed class BulkCreateEventsCommandHandler : IRequestHandler<BulkCreateEventsCommand, CsvImportResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly IEventImportValidationService _validationService;
    private readonly ILogger<BulkCreateEventsCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BulkCreateEventsCommandHandler"/> class.
    /// </summary>
    /// <param name="context">The application database context.</param>
    /// <param name="auditService">The audit logging service.</param>
    /// <param name="validationService">The batch normalization/validation/duplicate-detection service.</param>
    /// <param name="logger">The logger for structured, content-free diagnostics.</param>
    public BulkCreateEventsCommandHandler(
        IApplicationDbContext context,
        IAuditService auditService,
        IEventImportValidationService validationService,
        ILogger<BulkCreateEventsCommandHandler> logger)
    {
        _context = context;
        _auditService = auditService;
        _validationService = validationService;
        _logger = logger;
    }

    /// <summary>
    /// Handles the command by re-validating and then persisting every event row.
    /// </summary>
    /// <param name="request">The command containing the admin-approved event rows.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The import result, summarizing what was imported or why it was rejected.</returns>
    public async Task<CsvImportResultDto> Handle(BulkCreateEventsCommand request, CancellationToken cancellationToken)
    {
        var validationResults = await _validationService.ValidateAsync(request.Events, cancellationToken);
        var failedRows = BuildFailedRows(validationResults);

        if (failedRows.Count > 0)
        {
            _logger.LogWarning(
                "Bulk event import aborted for administrator {AdminUserId}: {FailedCount} of {TotalCount} row(s) failed re-validation.",
                request.AdminUserId,
                failedRows.Count,
                request.Events.Count);

            return new CsvImportResultDto
            {
                ImportedCount = 0,
                FailedCount = failedRows.Count,
                FailedRows = failedRows
            };
        }

        // The in-memory EF Core provider used by unit tests does not support real transactions,
        // so the transaction is only opened against a relational (production) database.
        await using var transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(cancellationToken)
            : null;

        // Persist the normalized item (trimmed and, if enabled, title-cased), not the raw
        // payload the admin submitted, so the normalization is reflected in what's stored.
        var newEvents = validationResults
            .Select(r => r.NormalizedItem)
            .Select(item => new Event
            {
                Title = item.Title,
                Description = item.Description,
                Date = item.Date,
                Location = item.Location,
                MaxCapacity = item.MaxCapacity
            })
            .ToList();

        foreach (var newEvent in newEvents)
        {
            // Domain-level defense in depth, mirroring CreateEventCommandHandler.
            newEvent.ValidateFutureDate();
        }

        _context.Events.AddRange(newEvents);

        var auditDetails = new
        {
            Count = newEvents.Count,
            Events = newEvents.Select(e => new { e.Title, e.Date }).ToList()
        };

        // Log the audit entry before saving changes, per the established convention.
        await _auditService.LogAsync(
            AuditAction.EventsImported,
            request.AdminUserId,
            Guid.Empty,
            $"{newEvents.Count} event(s) imported via CSV",
            JsonSerializer.Serialize(auditDetails),
            request.IpAddress,
            request.UserAgent,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        _logger.LogInformation(
            "Bulk event import committed for administrator {AdminUserId}: {ImportedCount} event(s) created.",
            request.AdminUserId,
            newEvents.Count);

        return new CsvImportResultDto
        {
            ImportedCount = newEvents.Count,
            FailedCount = 0,
            FailedRows = []
        };
    }

    /// <summary>
    /// Builds the failure list from the batch's re-validation results, if any. The row number
    /// reported here reflects the position of each row within the batch handed to this command,
    /// matching the row numbers used inside <see cref="IEventImportValidationService"/>'s own
    /// duplicate messages (e.g. "Duplicate of row 2").
    /// </summary>
    /// <param name="validationResults">The batch validation results, in submission order.</param>
    /// <returns>The rows that failed validation, each annotated with its error messages.</returns>
    private static List<CsvImportRowDto> BuildFailedRows(IReadOnlyList<ImportRowValidationResult> validationResults)
    {
        var failedRows = new List<CsvImportRowDto>();

        for (var i = 0; i < validationResults.Count; i++)
        {
            var result = validationResults[i];

            if (result.IsValid)
            {
                continue;
            }

            var item = result.NormalizedItem;

            failedRows.Add(new CsvImportRowDto
            {
                RowNumber = i + 1,
                Title = item.Title,
                Date = item.Date,
                Description = item.Description,
                Location = item.Location,
                MaxCapacity = item.MaxCapacity,
                IsValid = false,
                IsDuplicate = result.IsDuplicate,
                Errors = result.Errors
            });
        }

        return failedRows;
    }
}
