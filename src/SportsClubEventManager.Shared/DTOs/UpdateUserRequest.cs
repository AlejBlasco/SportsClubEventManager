using SportsClubEventManager.Domain.Enums;

namespace SportsClubEventManager.Shared.DTOs;

/// <summary>
/// Request model for updating user information by an administrator.
/// </summary>
public class UpdateUserRequest
{
    /// <summary>
    /// Gets or sets the name of the user.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email address of the user.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the gender of the user.
    /// </summary>
    public Gender Gender { get; set; }

    /// <summary>
    /// Gets or sets the license number of the user.
    /// </summary>
    public string? LicenseNumber { get; set; }

    /// <summary>
    /// Gets or sets the license category of the user.
    /// </summary>
    public string? LicenseCategory { get; set; }
}
