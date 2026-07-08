using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Domain.Enums;
using SportsClubEventManager.Domain.Exceptions;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Application.Events.Commands.UpdateEvent;

/// <summary>
/// Handler for updating an existing event.
/// </summary>
public class UpdateEventCommandHandler : IRequestHandler<UpdateEventCommand, EventAdminListDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateEventCommandHandler"/> class.
    /// </summary>
    /// <param name="context">The application database context.</param>
    /// <param name="auditService">The audit logging service.</param>
    public UpdateEventCommandHandler(IApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    /// <summary>
    /// Handles the command to update event information and create an audit log entry.
    /// </summary>
    /// <param name="request">The command containing updated event information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated event details.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the event is not found.</exception>
    /// <exception cref="DomainException">Thrown when validation rules are violated.</exception>
    /// <exception cref="DbUpdateConcurrencyException">Thrown when a concurrency conflict occurs.</exception>
    public async Task<EventAdminListDto> Handle(UpdateEventCommand request, CancellationToken cancellationToken)
    {
        var eventEntity = await _context.Events
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Id == request.EventId, cancellationToken);

        if (eventEntity == null)
        {
            throw new KeyNotFoundException($"Event with ID {request.EventId} was not found.");
        }

        // OQ-1: Past events are read-only
        if (eventEntity.Date < DateTime.UtcNow)
        {
            throw new DomainException("Past events cannot be modified.");
        }

        // Capture old values for audit trail
        var changes = new
        {
            OldValues = new
            {
                eventEntity.Title,
                eventEntity.Description,
                eventEntity.Date,
                eventEntity.Location,
                eventEntity.MaxCapacity
            },
            NewValues = new
            {
                request.Title,
                request.Description,
                request.Date,
                request.Location,
                request.MaxCapacity
            }
        };

        // Update event properties
        eventEntity.Title = request.Title;
        eventEntity.Description = request.Description;
        eventEntity.Date = request.Date;
        eventEntity.Location = request.Location;
        eventEntity.MaxCapacity = request.MaxCapacity;

        // Domain validation
        eventEntity.ValidateFutureDate();

        // Set RowVersion for optimistic concurrency
        if (request.RowVersion != null)
        {
            _context.Events.Entry(eventEntity).OriginalValues[nameof(eventEntity.RowVersion)] = request.RowVersion;
        }

        // Log the audit entry before saving changes
        await _auditService.LogAsync(
            AuditAction.EventUpdated,
            request.AdminUserId,
            eventEntity.Id,
            eventEntity.Title,
            JsonSerializer.Serialize(changes),
            request.IpAddress,
            request.UserAgent,
            cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException("The event was modified by another user. Please reload and try again.");
        }

        return new EventAdminListDto
        {
            Id = eventEntity.Id,
            Title = eventEntity.Title,
            Date = eventEntity.Date,
            Location = eventEntity.Location,
            MaxCapacity = eventEntity.MaxCapacity,
            CurrentRegistrations = eventEntity.CurrentRegistrations,
            IsPastEvent = eventEntity.Date < DateTime.UtcNow,
            CreatedAt = eventEntity.CreatedAt,
            RowVersion = eventEntity.RowVersion
        };
    }
}
