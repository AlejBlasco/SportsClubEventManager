using MediatR;
using Microsoft.EntityFrameworkCore;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Domain.Enums;

namespace SportsClubEventManager.Application.Users.Commands.DeleteUser;

/// <summary>
/// Handler for permanently deleting a user account with cascade deletion of related registrations.
/// </summary>
public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteUserCommandHandler"/> class.
    /// </summary>
    /// <param name="context">The application database context.</param>
    /// <param name="auditService">The audit logging service.</param>
    public DeleteUserCommandHandler(IApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    /// <summary>
    /// Handles the command to delete a user account and create an audit log entry.
    /// </summary>
    /// <param name="request">The command containing the user ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the user is not found.</exception>
    /// <exception cref="InvalidOperationException">Thrown when attempting to delete the last Administrator.</exception>
    public async Task<Unit> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Include(u => u.Registrations)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {request.UserId} was not found.");
        }

        // Prevent deletion of the last Administrator
        if (user.Role == Role.Administrator)
        {
            var adminCount = await _context.Users
                .CountAsync(u => u.Role == Role.Administrator, cancellationToken);

            if (adminCount <= 1)
            {
                throw new InvalidOperationException("Cannot delete the last administrator in the system.");
            }
        }

        // Capture email before deletion for audit trail
        var userEmail = user.Email;

        // Log the deletion before removing the user
        await _auditService.LogAsync(
            AuditAction.UserDeleted,
            request.AdminUserId,
            user.Id,
            userEmail,
            ipAddress: request.IpAddress,
            userAgent: request.UserAgent,
            cancellationToken: cancellationToken);

        // Cascade delete registrations (EF Core will handle this if configured properly,
        // but we explicitly remove them here for clarity)
        _context.Registrations.RemoveRange(user.Registrations);

        // Delete the user
        _context.Users.Remove(user);

        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
