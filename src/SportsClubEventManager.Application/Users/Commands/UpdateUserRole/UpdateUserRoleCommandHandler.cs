using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Domain.Enums;

namespace SportsClubEventManager.Application.Users.Commands.UpdateUserRole;

/// <summary>
/// Handler for updating a user's role with validation to ensure at least one Administrator always exists.
/// </summary>
public class UpdateUserRoleCommandHandler : IRequestHandler<UpdateUserRoleCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateUserRoleCommandHandler"/> class.
    /// </summary>
    /// <param name="context">The application database context.</param>
    /// <param name="auditService">The audit logging service.</param>
    public UpdateUserRoleCommandHandler(IApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    /// <summary>
    /// Handles the command to change a user's role and create an audit log entry.
    /// </summary>
    /// <param name="request">The command containing the user ID and new role.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the user is not found.</exception>
    /// <exception cref="InvalidOperationException">Thrown when attempting to remove the last Administrator.</exception>
    public async Task<Unit> Handle(UpdateUserRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {request.UserId} was not found.");
        }

        var oldRole = user.Role;

        // If removing Administrator role, check if this is the last Administrator
        if (oldRole == Role.Administrator && request.NewRole != Role.Administrator)
        {
            var adminCount = await _context.Users
                .CountAsync(u => u.Role == Role.Administrator, cancellationToken);

            if (adminCount <= 1)
            {
                throw new InvalidOperationException("Cannot remove the Administrator role from the last administrator in the system.");
            }
        }

        user.Role = request.NewRole;

        var changes = new
        {
            OldRole = oldRole.ToString(),
            NewRole = request.NewRole.ToString()
        };

        var action = request.NewRole == Role.Administrator ? AuditAction.RoleAssigned : AuditAction.RoleRemoved;

        await _auditService.LogAsync(
            action,
            request.AdminUserId,
            user.Id,
            user.Email,
            JsonSerializer.Serialize(changes),
            request.IpAddress,
            request.UserAgent,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
