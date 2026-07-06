namespace SportsClubEventManager.Shared.DTOs;

/// <summary>
/// Request model for updating user profile information.
/// </summary>
public sealed record UpdateProfileRequest
{
    /// <summary>
    /// Gets the name of the user.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the gender of the user.
    /// </summary>
    public string Gender { get; init; } = string.Empty;

    /// <summary>
    /// Gets the email address of the user.
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Gets the license number of the user.
    /// </summary>
    public string? LicenseNumber { get; init; }

    /// <summary>
    /// Gets the license category of the user.
    /// </summary>
    public string? LicenseCategory { get; init; }
}
