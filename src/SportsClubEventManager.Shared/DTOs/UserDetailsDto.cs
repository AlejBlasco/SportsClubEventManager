using SportsClubEventManager.Domain.Enums;

namespace SportsClubEventManager.Shared.DTOs;

/// <summary>
/// Data transfer object representing detailed user information for administrative editing.
/// </summary>
public class UserDetailsDto
{
    /// <summary>
    /// Gets or sets the unique identifier of the user.
    /// </summary>
    public Guid Id { get; set; }

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
    /// Gets or sets the role assigned to the user.
    /// </summary>
    public Role Role { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user account is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the license number of the user, if applicable.
    /// </summary>
    public string? LicenseNumber { get; set; }

    /// <summary>
    /// Gets or sets the license category of the user, if applicable.
    /// </summary>
    public string? LicenseCategory { get; set; }

    /// <summary>
    /// Gets or sets the authentication provider name (e.g., "Local", "Google", "Microsoft").
    /// </summary>
    public string? ProviderName { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the user account was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the user's last login, if available.
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Gets or sets the count of registrations associated with this user.
    /// </summary>
    public int RegistrationCount { get; set; }
}
