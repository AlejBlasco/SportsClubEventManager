namespace SportsClubEventManager.Shared.DTOs;

/// <summary>
/// Data transfer object for user profile information.
/// </summary>
public sealed record UserProfileDto
{
    /// <summary>
    /// Gets the unique identifier of the user.
    /// </summary>
    public Guid UserId { get; init; }

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

    /// <summary>
    /// Gets the role of the user.
    /// </summary>
    public string Role { get; init; } = string.Empty;

    /// <summary>
    /// Gets the date when the user was created.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Gets the date when the user profile was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; init; }

    /// <summary>
    /// Gets a value indicating whether the user is authenticated via OAuth2 provider.
    /// </summary>
    public bool IsOAuthUser { get; init; }

    /// <summary>
    /// Gets the name of the authentication provider (e.g., "Google", "Local").
    /// </summary>
    public string? ProviderName { get; init; }
}
