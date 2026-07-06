namespace SportsClubEventManager.Shared.DTOs;

/// <summary>
/// Data transfer object for login responses.
/// </summary>
public sealed class LoginResponse
{
    /// <summary>
    /// Gets or sets the unique identifier of the authenticated user.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the email address of the authenticated user.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the authenticated user.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the role of the authenticated user.
    /// Valid values are: "User", "Administrator"
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the JWT access token.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the refresh token for session renewal.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the expiration time of the access token in seconds.
    /// </summary>
    public int ExpiresIn { get; set; }
}
