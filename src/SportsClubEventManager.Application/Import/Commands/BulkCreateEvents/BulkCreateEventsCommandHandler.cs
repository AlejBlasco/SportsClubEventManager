using System.Text.Json;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Domain.Entities;
using SportsClubEventManager.Domain.Enums;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Application.Import.Commands.BulkCreateEvents;

/// <summary>
/// Handler for confirming a CSV import. Re-validates every row as defense-in-depth against a
/// stale or tampered client payload, then inserts all events in a single, all-or-nothing
/// database transaction and writes one summary <c>AuditLog</c> entry.
/// </summary>
public sealed class BulkCreateEventsCommandHandler : IRequestHandler<BulkCreateEventsCommand, CsvImportResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly IValidator<ImportEventItemDto> _itemValidator;
    private readonly ILogger<BulkCreateEventsCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BulkCreateEventsCommandHandler"/> class.
    /// </summary>
    /// <param name="context">The application database context.</param>
    /// <param name="auditService">The audit logging service.</param>
    /// <param name="itemValidator">The shared field-level validator for mapped event rows.</param>
    /// <param name="logger">The logger for structured, content-free diagnostics.</param>
    public BulkCreateEventsCommandHandler(
        IApplicationDbContext context,
        IAuditService auditService,
        IValidator<ImportEventItemDto> itemValidator,
        ILogger<BulkCreateEventsCommandHandler> logger)
    {
        _context = context;
        _auditService = auditService;
        _itemValidator = itemValidator;
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
        var failedRows = BuildFailedRows(request.Events);

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

        var newEvents = request.Events
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
    /// Re-validates every requested row and builds the failure list, if any.
    /// </summary>
    /// <param name="events">The event rows to validate.</param>
    /// <returns>The rows that failed validation, each annotated with its error messages.</returns>
    private List<CsvImportRowDto> BuildFailedRows(IReadOnlyList<ImportEventItemDto> events)
    {
        var failedRows = new List<CsvImportRowDto>();

        for (var i = 0; i < events.Count; i++)
        {
            var item = events[i];
            var validationResult = _itemValidator.Validate(item);

            if (validationResult.IsValid)
            {
                continue;
            }

            failedRows.Add(new CsvImportRowDto
            {
                RowNumber = i + 1,
                Title = item.Title,
                Date = item.Date,
                Description = item.Description,
                Location = item.Location,
                MaxCapacity = item.MaxCapacity,
                IsValid = false,
                Errors = validationResult.Errors.Select(e => e.ErrorMessage).Distinct().ToList()
            });
        }

        return failedRows;
    }
}
