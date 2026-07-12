using MediatR;
using Microsoft.EntityFrameworkCore;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Domain.Entities;
using SportsClubEventManager.Domain.Enums;
using SportsClubEventManager.Domain.Exceptions;

namespace SportsClubEventManager.Application.Registrations.Commands.CreateAdminRegistration;

/// <summary>
/// Handler for creating registrations as administrator.
/// </summary>
public sealed class CreateAdminRegistrationCommandHandler(
    IApplicationDbContext context,
    IAuditService auditService,
    IApplicationMetrics metrics) : IRequestHandler<CreateAdminRegistrationCommand, Guid>
{
    /// <summary>
    /// Handles the command and creates a new registration.
    /// </summary>
    /// <param name="request">The command request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created registration identifier.</returns>
    public async Task<Guid> Handle(CreateAdminRegistrationCommand request, CancellationToken cancellationToken)
    {
        var eventEntity = await context.Events
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Id == request.EventId, cancellationToken);

        if (eventEntity is null)
        {
            throw new EntityNotFoundException($"Event with identifier '{request.EventId}' does not exist.");
        }

        if (eventEntity.Date < DateTime.UtcNow)
        {
            throw new DomainException("Cannot register users for events that have already occurred.");
        }

        var userEntity = await context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (userEntity is null)
        {
            throw new EntityNotFoundException($"User with identifier '{request.UserId}' does not exist.");
        }

        if (!userEntity.IsActive)
        {
            throw new InvalidOperationException("Cannot register an inactive user.");
        }

        var existingRegistration = eventEntity.Registrations
            .FirstOrDefault(r => r.UserId == request.UserId && r.Status != RegistrationStatus.Cancelled);

        if (existingRegistration is not null)
        {
            throw new DuplicateRegistrationException("User is already registered for this event.");
        }

        var activeRegistrationsCount = eventEntity.Registrations.Count(r => r.Status != RegistrationStatus.Cancelled);
        if (activeRegistrationsCount >= eventEntity.MaxCapacity)
        {
            throw new CapacityExceededException("Event has reached maximum capacity.");
        }

        var registration = new Registration
        {
            EventId = eventEntity.Id,
            UserId = userEntity.Id,
            RegistrationDate = DateTime.UtcNow,
            Status = RegistrationStatus.Registered
        };

        context.Registrations.Add(registration);

        await auditService.LogAsync(
            AuditAction.RegistrationCreated,
            request.AdminUserId,
            userEntity.Id,
            userEntity.Email,
            changes: $"{{\"eventId\":\"{eventEntity.Id}\",\"eventTitle\":\"{eventEntity.Title}\",\"registrationId\":\"{registration.Id}\"}}",
            ipAddress: request.IpAddress,
            userAgent: request.UserAgent,
            cancellationToken: cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        // Recorded only after a successful SaveChangesAsync, so operations that roll back are
        // never counted (issue #42).
        metrics.RecordRegistrationCreated("admin");

        return registration.Id;
    }
}
