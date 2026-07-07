using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Domain.Enums;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Application.Users.Commands.UpdateUserAsAdmin;

/// <summary>
/// Handler for updating user information by an administrator.
/// </summary>
public class UpdateUserAsAdminCommandHandler : IRequestHandler<UpdateUserAsAdminCommand, UserDetailsDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateUserAsAdminCommandHandler"/> class.
    /// </summary>
    /// <param name="context">The application database context.</param>
    /// <param name="auditService">The audit logging service.</param>
    public UpdateUserAsAdminCommandHandler(IApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    /// <summary>
    /// Handles the command to update user information and create an audit log entry.
    /// </summary>
    /// <param name="request">The command containing updated user information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated user details.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the user is not found.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the email is already in use by another user.</exception>
    public async Task<UserDetailsDto> Handle(UpdateUserAsAdminCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Include(u => u.Registrations)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {request.UserId} was not found.");
        }

        // Check email uniqueness if email is being changed
        if (user.Email != request.Email)
        {
            var emailExists = await _context.Users
                .AnyAsync(u => u.Email == request.Email && u.Id != request.UserId, cancellationToken);

            if (emailExists)
            {
                throw new InvalidOperationException("The email address is already in use by another user.");
            }
        }

        // Capture old values for audit trail
        var changes = new
        {
            OldValues = new
            {
                user.Name,
                user.Email,
                user.Gender,
                user.LicenseNumber,
                user.LicenseCategory
            },
            NewValues = new
            {
                request.Name,
                request.Email,
                request.Gender,
                request.LicenseNumber,
                request.LicenseCategory
            }
        };

        // Update user properties
        user.Name = request.Name;
        user.Email = request.Email;
        user.Gender = request.Gender;
        user.LicenseNumber = request.LicenseNumber;
        user.LicenseCategory = request.LicenseCategory;

        // Log the audit entry before saving changes
        await _auditService.LogAsync(
            AuditAction.UserUpdated,
            request.AdminUserId,
            user.Id,
            user.Email,
            JsonSerializer.Serialize(changes),
            request.IpAddress,
            request.UserAgent,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return new UserDetailsDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Gender = user.Gender,
            Role = user.Role,
            IsActive = user.IsActive,
            LicenseNumber = user.LicenseNumber,
            LicenseCategory = user.LicenseCategory,
            ProviderName = user.ProviderName,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            RegistrationCount = user.Registrations.Count
        };
    }
}
