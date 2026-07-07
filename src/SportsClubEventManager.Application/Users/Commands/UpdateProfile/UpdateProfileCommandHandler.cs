using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Domain.Enums;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Application.Users.Commands.UpdateProfile;

/// <summary>
/// Handler for updating user profile information.
/// </summary>
public sealed class UpdateProfileCommandHandler(
    IApplicationDbContext context,
    ILogger<UpdateProfileCommandHandler> logger)
    : IRequestHandler<UpdateProfileCommand, UserProfileDto>
{
    /// <summary>
    /// Handles the command to update user profile information.
    /// </summary>
    /// <param name="request">The command containing updated profile data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated user profile data transfer object.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when user attempts to modify another user's profile.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when the user is not found.</exception>
    /// <exception cref="InvalidOperationException">Thrown when validation fails (e.g., email already in use, OAuth user attempts email change).</exception>
    public async Task<UserProfileDto> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        // Authorization check: user can only update their own profile
        if (request.RequestingUserId != request.UserId)
        {
            logger.LogWarning(
                "Unauthorized profile update attempt. User {RequestingUserId} attempted to modify user {TargetUserId}",
                request.RequestingUserId,
                request.UserId);
            throw new UnauthorizedAccessException("You can only update your own profile.");
        }

        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            throw new KeyNotFoundException($"User with ID {request.UserId} not found.");
        }

        // OAuth users cannot change their email
        if (!string.IsNullOrEmpty(user.ProviderName) && user.Email != request.Email)
        {
            throw new InvalidOperationException(
                $"Email is managed by {user.ProviderName} and cannot be changed here.");
        }

        // Check email uniqueness (for local auth users changing email)
        if (user.Email != request.Email)
        {
            var emailExists = await context.Users
                .AnyAsync(u => u.Email == request.Email && u.Id != user.Id, cancellationToken);

            if (emailExists)
            {
                throw new InvalidOperationException("This email address is already registered.");
            }
        }

        // Parse Gender enum
        if (!Enum.TryParse<Gender>(request.Gender, ignoreCase: true, out var gender))
        {
            throw new InvalidOperationException($"Invalid gender value: {request.Gender}");
        }

        // Update user properties
        user.Name = request.Name;
        user.Gender = gender;
        user.Email = request.Email;
        user.LicenseNumber = request.LicenseNumber;
        user.LicenseCategory = request.LicenseCategory;

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "User {UserId} updated profile. Fields: Name, Gender, Email, LicenseNumber, LicenseCategory",
            user.Id);

        return new UserProfileDto
        {
            UserId = user.Id,
            Name = user.Name,
            Gender = user.Gender.ToString(),
            Email = user.Email,
            LicenseNumber = user.LicenseNumber,
            LicenseCategory = user.LicenseCategory,
            Role = user.Role.ToString(),
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt ?? user.CreatedAt,
            IsOAuthUser = !string.IsNullOrEmpty(user.ProviderName),
            ProviderName = user.ProviderName
        };
    }
}
