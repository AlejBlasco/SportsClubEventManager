using MediatR;
using SportsClubEventManager.Domain.Enums;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Application.Users.Commands.UpdateUserAsAdmin;

/// <summary>
/// Command to update user information as an administrator.
/// </summary>
public class UpdateUserAsAdminCommand : IRequest<UserDetailsDto>
{
    /// <summary>
    /// Gets or sets the ID of the administrator performing the update.
    /// </summary>
    public Guid AdminUserId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user to update.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the new name of the user.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the new email address of the user.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the new gender of the user.
    /// </summary>
    public Gender Gender { get; set; }

    /// <summary>
    /// Gets or sets the new license number of the user.
    /// </summary>
    public string? LicenseNumber { get; set; }

    /// <summary>
    /// Gets or sets the new license category of the user.
    /// </summary>
    public string? LicenseCategory { get; set; }

    /// <summary>
    /// Gets or sets the IP address of the administrator, if available.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the User-Agent string of the client, if available.
    /// </summary>
    public string? UserAgent { get; set; }
}
