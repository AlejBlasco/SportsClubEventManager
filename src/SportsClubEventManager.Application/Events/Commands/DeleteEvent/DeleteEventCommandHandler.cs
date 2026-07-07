using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Domain.Enums;
using SportsClubEventManager.Domain.Exceptions;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Application.Events.Commands.DeleteEvent;

/// <summary>
/// Handler for deleting an event and cancelling all associated registrations.
/// </summary>
public class DeleteEventCommandHandler : IRequestHandler<DeleteEventCommand, DeleteEventResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteEventCommandHandler"/> class.
    /// </summary>
    /// <param name="context">The application database context.</param>
    /// <param name="auditService">The audit logging service.</param>
    public DeleteEventCommandHandler(IApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    /// <summary>
    /// Handles the command to delete an event and cancel all registrations atomically.
    /// </summary>
    /// <param name="request">The command containing event deletion information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A response indicating the result of the deletion operation.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the event is not found.</exception>
    /// <exception cref="DomainException">Thrown when validation rules are violated.</exception>
    public async Task<DeleteEventResponse> Handle(DeleteEventCommand request, CancellationToken cancellationToken)
    {
        var eventEntity = await _context.Events
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Id == request.EventId, cancellationToken);

        if (eventEntity == null)
        {
            throw new KeyNotFoundException($"Event with ID {request.EventId} was not found.");
        }

        // OQ-1: Past events are read-only (cannot be deleted)
        if (eventEntity.Date < DateTime.UtcNow)
        {
            throw new DomainException("Past events cannot be deleted.");
        }

        var activeRegistrationsCount = eventEntity.Registrations
            .Count(r => r.Status != RegistrationStatus.Cancelled);

        // Begin explicit transaction for atomic operation
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Cancel all active registrations using bulk update for performance (NFR-2: 5-second SLA for 500+ registrations)
            // Use ExecuteUpdateAsync to avoid N+1 performance issue and check cancellation token
            var cancelledCount = await _context.Registrations
                .Where(r => r.EventId == request.EventId && r.Status != RegistrationStatus.Cancelled)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(r => r.Status, RegistrationStatus.Cancelled),
                    cancellationToken);

            // Create audit details
            var auditDetails = new
            {
                EventId = eventEntity.Id,
                Title = eventEntity.Title,
                Date = eventEntity.Date,
                Location = eventEntity.Location,
                CancelledRegistrations = activeRegistrationsCount
            };

            // Log the audit entry
            await _auditService.LogAsync(
                AuditAction.EventDeleted,
                request.AdminUserId,
                eventEntity.Id,
                eventEntity.Title,
                JsonSerializer.Serialize(auditDetails),
                request.IpAddress,
                request.UserAgent,
                cancellationToken);

            // OQ-3: Hard delete the event
            _context.Events.Remove(eventEntity);

            await _context.SaveChangesAsync(cancellationToken);

            // Commit the transaction
            await transaction.CommitAsync(cancellationToken);

            return new DeleteEventResponse
            {
                Success = true,
                CancelledRegistrationsCount = activeRegistrationsCount,
                Message = activeRegistrationsCount > 0
                    ? $"Event deleted successfully. {activeRegistrationsCount} registration(s) were cancelled."
                    : "Event deleted successfully."
            };
        }
        catch (Exception)
        {
            // Rollback on any error
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
