using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SportsClubEventManager.Application.Authentication.Common;
using SportsClubEventManager.Application.Common.Interfaces;

namespace SportsClubEventManager.Application.Users.Commands.ChangePassword;

/// <summary>
/// Handler for changing user password.
/// </summary>
public sealed class ChangePasswordCommandHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IConfiguration configuration,
    IDateTimeProvider dateTimeProvider,
    ILogger<ChangePasswordCommandHandler> logger)
    : IRequestHandler<ChangePasswordCommand, AuthenticationResult>
{
    /// <summary>
    /// Handles the command to change user password.
    /// </summary>
    /// <param name="request">The command containing current and new password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>New authentication tokens.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when user attempts to change another user's password or current password is incorrect.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when the user is not found.</exception>
    /// <exception cref="InvalidOperationException">Thrown when OAuth user attempts password change.</exception>
    public async Task<AuthenticationResult> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        // Authorization check: user can only change their own password
        if (request.RequestingUserId != request.UserId)
        {
            logger.LogWarning(
                "Unauthorized password change attempt. User {RequestingUserId} attempted to modify user {TargetUserId}",
                request.RequestingUserId,
                request.UserId);
            throw new UnauthorizedAccessException("You can only change your own password.");
        }

        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            throw new KeyNotFoundException($"User with ID {request.UserId} not found.");
        }

        // OAuth users cannot change password
        if (!string.IsNullOrEmpty(user.ProviderName))
        {
            throw new InvalidOperationException(
                $"Password is managed by {user.ProviderName} and cannot be changed here.");
        }

        // Verify current password
        if (string.IsNullOrEmpty(user.PasswordHash) ||
            !passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            logger.LogWarning("Failed password change attempt for user {UserId}: incorrect current password", user.Id);
            throw new UnauthorizedAccessException("Current password is incorrect.");
        }

        // Hash and store new password
        user.PasswordHash = passwordHasher.HashPassword(request.NewPassword);

        // Generate new tokens
        var accessToken = tokenService.GenerateAccessToken(user.Id, user.Email, user.Name, user.Role);
        var refreshToken = tokenService.GenerateRefreshToken();
        var hashedRefreshToken = tokenService.HashRefreshToken(refreshToken);

        user.RefreshToken = hashedRefreshToken;
        user.RefreshTokenExpiryTime = dateTimeProvider.UtcNow.AddDays(
            configuration.GetValue<int>("Authentication:JwtSettings:RefreshTokenExpirationDays", 7));

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} changed password successfully", user.Id);

        var expiresIn = configuration.GetValue<int>("Authentication:JwtSettings:AccessTokenExpirationMinutes", 30) * 60;

        return new AuthenticationResult
        {
            UserId = user.Id,
            Email = user.Email,
            Name = user.Name,
            Role = user.Role,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = expiresIn
        };
    }
}
