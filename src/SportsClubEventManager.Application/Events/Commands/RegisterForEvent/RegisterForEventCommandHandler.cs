using MediatR;
using Microsoft.EntityFrameworkCore;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Domain.Entities;
using SportsClubEventManager.Domain.Enums;
using SportsClubEventManager.Domain.Exceptions;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Application.Events.Commands.RegisterForEvent;

/// <summary>
/// Handles the RegisterForEventCommand by creating a new registration for a user to an event.
/// </summary>
public sealed class RegisterForEventCommandHandler(IApplicationDbContext context, IApplicationMetrics metrics)
    : IRequestHandler<RegisterForEventCommand, RegistrationCreatedDto>
{
    /// <summary>
    /// Handles the command execution.
    /// </summary>
    /// <param name="request">The command request containing the event and user identifiers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The registration created DTO with full event details.</returns>
    /// <exception cref="EntityNotFoundException">Thrown when the event does not exist.</exception>
    /// <exception cref="DuplicateRegistrationException">Thrown when the user is already registered for the event.</exception>
    /// <exception cref="CapacityExceededException">Thrown when the event has reached maximum capacity.</exception>
    /// <exception cref="DomainException">Thrown when validation fails or business rules are violated.</exception>
    public async Task<RegistrationCreatedDto> Handle(RegisterForEventCommand request, CancellationToken cancellationToken)
    {
        // Load event with registrations to check capacity and duplicates
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
            throw new DomainException("Cannot register for events that have already occurred.");
        }

        // Check if user already has an active registration for this event
        var existingActiveRegistration = eventEntity.Registrations
            .FirstOrDefault(r => r.UserId == request.UserId && r.Status != RegistrationStatus.Cancelled);

        if (existingActiveRegistration is not null)
        {
            throw new DuplicateRegistrationException($"User is already registered for this event with status '{existingActiveRegistration.Status}'.");
        }

        // Check if event has available capacity
        var activeRegistrationsCount = eventEntity.Registrations
            .Count(r => r.Status != RegistrationStatus.Cancelled);

        if (activeRegistrationsCount >= eventEntity.MaxCapacity)
        {
            throw new CapacityExceededException("Event has reached maximum capacity.");
        }

        // Create new registration
        var registration = new Registration
        {
            EventId = request.EventId,
            UserId = request.UserId,
            RegistrationDate = DateTime.UtcNow,
            Status = RegistrationStatus.Registered
        };

        context.Registrations.Add(registration);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Concurrency conflict occurred - likely another registration was created simultaneously
            throw new DomainException("Event capacity was reached while processing this registration. Please try again.");
        }

        // Recorded only after a successful SaveChangesAsync, so operations that roll back are
        // never counted (issue #42).
        metrics.RecordRegistrationCreated("self-service");

        // Return DTO with full event details
        return new RegistrationCreatedDto
        {
            RegistrationId = registration.Id,
            EventId = eventEntity.Id,
            UserId = request.UserId,
            RegisteredAt = registration.RegistrationDate,
            Status = registration.Status,
            Event = new EventDetailDto
            {
                Id = eventEntity.Id,
                Title = eventEntity.Title,
                Description = eventEntity.Description,
                Date = eventEntity.Date,
                Location = eventEntity.Location,
                MaxCapacity = eventEntity.MaxCapacity,
                CurrentRegistrations = activeRegistrationsCount + 1, // Include the new registration
                AvailableSlots = eventEntity.MaxCapacity - (activeRegistrationsCount + 1),
                IsFullyBooked = (activeRegistrationsCount + 1) >= eventEntity.MaxCapacity
            }
        };
    }
}
