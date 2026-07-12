using MediatR;
using Microsoft.EntityFrameworkCore;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Domain.Enums;
using SportsClubEventManager.Domain.Exceptions;

namespace SportsClubEventManager.Application.Registrations.Commands.CancelRegistrationById;

/// <summary>
/// Handler for cancelling registrations by identifier.
/// </summary>
public sealed class CancelRegistrationByIdCommandHandler(
    IApplicationDbContext context,
    IAuditService auditService,
    IApplicationMetrics metrics) : IRequestHandler<CancelRegistrationByIdCommand>
{
    /// <summary>
    /// Handles the cancellation command.
    /// </summary>
    /// <param name="request">The cancellation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task Handle(CancelRegistrationByIdCommand request, CancellationToken cancellationToken)
    {
        var registration = await context.Registrations
            .Include(r => r.Event)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == request.RegistrationId, cancellationToken);

        if (registration is null)
        {
            throw new EntityNotFoundException($"Registration with identifier '{request.RegistrationId}' does not exist.");
        }

        if (!request.IsAdministrator && registration.UserId != request.RequestingUserId)
        {
            throw new UnauthorizedAccessException("Users can only cancel their own registrations.");
        }

        if (!request.IsAdministrator && registration.Event.Date < DateTime.UtcNow)
        {
            throw new DomainException("Cannot cancel registrations for events that have already occurred.");
        }

        context.Registrations.Remove(registration);

        if (request.IsAdministrator)
        {
            await auditService.LogAsync(
                AuditAction.RegistrationCancelled,
                request.RequestingUserId,
                registration.UserId,
                registration.User.Email,
                changes: $"{{\"registrationId\":\"{registration.Id}\",\"eventId\":\"{registration.EventId}\",\"eventTitle\":\"{registration.Event.Title}\"}}",
                ipAddress: request.IpAddress,
                userAgent: request.UserAgent,
                cancellationToken: cancellationToken);
        }

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new DomainException("The registration was modified or deleted by another process. Please try again.");
        }

        // Recorded only after a successful SaveChangesAsync, so operations that roll back are
        // never counted (issue #42).
        metrics.RecordRegistrationCancelled(request.IsAdministrator ? "admin" : "self-service");
    }
}
