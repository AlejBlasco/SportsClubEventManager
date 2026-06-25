using MediatR;
using Microsoft.EntityFrameworkCore;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Domain.Enums;
using SportsClubEventManager.Domain.Exceptions;

namespace SportsClubEventManager.Application.Events.Commands.CancelRegistration;

/// <summary>
/// Handles the CancelRegistrationCommand by removing a user's registration for an event.
/// </summary>
public sealed class CancelRegistrationCommandHandler(IApplicationDbContext context)
    : IRequestHandler<CancelRegistrationCommand>
{
    /// <summary>
    /// Handles the command execution.
    /// </summary>
    /// <param name="request">The command request containing the event and user identifiers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="EntityNotFoundException">Thrown when the event or registration does not exist.</exception>
    /// <exception cref="DomainException">Thrown when validation fails or business rules are violated.</exception>
    public async Task Handle(CancelRegistrationCommand request, CancellationToken cancellationToken)
    {
        // Load event with registrations to check existence and validate date
        var eventEntity = await context.Events
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Id == request.EventId, cancellationToken);

        if (eventEntity is null)
        {
            throw new EntityNotFoundException($"Event with identifier '{request.EventId}' does not exist.");
        }

        // Validate event date is in the future
        if (eventEntity.Date < DateTime.UtcNow)
        {
            throw new DomainException("Cannot cancel registrations for events that have already occurred.");
        }

        // Find the active registration for this user and event
        var registration = eventEntity.Registrations
            .FirstOrDefault(r => r.UserId == request.UserId && r.Status != RegistrationStatus.Cancelled);

        if (registration is null)
        {
            throw new EntityNotFoundException($"No active registration found for user '{request.UserId}' and event '{request.EventId}'.");
        }

        // Hard delete the registration
        context.Registrations.Remove(registration);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Concurrency conflict occurred - likely the registration was already deleted
            throw new DomainException("The registration was modified or deleted by another process. Please try again.");
        }
    }
}
