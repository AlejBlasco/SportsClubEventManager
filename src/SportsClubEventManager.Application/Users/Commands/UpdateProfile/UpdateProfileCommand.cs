using MediatR;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Application.Users.Commands.UpdateProfile;

/// <summary>
/// Command to update user profile information.
/// </summary>
public sealed record UpdateProfileCommand : IRequest<UserProfileDto>
{
    /// <summary>
    /// Gets the unique identifier of the user making the request (from JWT claims).
    /// </summary>
    public Guid RequestingUserId { get; init; }

    /// <summary>
    /// Gets the unique identifier of the user whose profile is being updated.
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// Gets the updated name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the updated gender.
    /// </summary>
    public string Gender { get; init; } = string.Empty;

    /// <summary>
    /// Gets the updated email address.
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Gets the updated license number.
    /// </summary>
    public string? LicenseNumber { get; init; }

    /// <summary>
    /// Gets the updated license category.
    /// </summary>
    public string? LicenseCategory { get; init; }
}
