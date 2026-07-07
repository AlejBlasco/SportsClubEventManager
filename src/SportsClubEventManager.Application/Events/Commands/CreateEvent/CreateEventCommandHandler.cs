using System.Text.Json;
using MediatR;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Domain.Entities;
using SportsClubEventManager.Domain.Enums;

namespace SportsClubEventManager.Application.Events.Commands.CreateEvent;

/// <summary>
/// Handler for creating a new event.
/// </summary>
public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateEventCommandHandler"/> class.
    /// </summary>
    /// <param name="context">The application database context.</param>
    /// <param name="auditService">The audit logging service.</param>
    public CreateEventCommandHandler(IApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    /// <summary>
    /// Handles the command to create a new event and create an audit log entry.
    /// </summary>
    /// <param name="request">The command containing event information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ID of the newly created event.</returns>
    public async Task<Guid> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        var newEvent = new Event
        {
            Title = request.Title,
            Description = request.Description,
            Date = request.Date,
            Location = request.Location,
            MaxCapacity = request.MaxCapacity
        };

        // Domain validation
        newEvent.ValidateFutureDate();

        _context.Events.Add(newEvent);

        // Create audit details
        var auditDetails = new
        {
            Title = request.Title,
            Date = request.Date,
            Location = request.Location,
            MaxCapacity = request.MaxCapacity,
            Description = request.Description
        };

        // Log the audit entry before saving changes
        await _auditService.LogAsync(
            AuditAction.EventCreated,
            request.AdminUserId,
            newEvent.Id,
            newEvent.Title,
            JsonSerializer.Serialize(auditDetails),
            request.IpAddress,
            request.UserAgent,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return newEvent.Id;
    }
}
