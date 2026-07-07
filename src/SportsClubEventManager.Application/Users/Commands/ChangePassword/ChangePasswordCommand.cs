using MediatR;
using SportsClubEventManager.Application.Authentication.Common;

namespace SportsClubEventManager.Application.Users.Commands.ChangePassword;

/// <summary>
/// Command to change user password.
/// </summary>
public sealed record ChangePasswordCommand : IRequest<AuthenticationResult>
{
    /// <summary>
    /// Gets the unique identifier of the user making the request (from JWT claims).
    /// </summary>
    public Guid RequestingUserId { get; init; }

    /// <summary>
    /// Gets the unique identifier of the user whose password is being changed.
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// Gets the current password for verification.
    /// </summary>
    public string CurrentPassword { get; init; } = string.Empty;

    /// <summary>
    /// Gets the new password to set.
    /// </summary>
    public string NewPassword { get; init; } = string.Empty;
}
