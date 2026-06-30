namespace SportsClubEventManager.Shared.DTOs;

/// <summary>
/// Data transfer object for login requests.
/// </summary>
public sealed class LoginRequest
{
    /// <summary>
    /// Gets or sets the email address of the user.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password of the user.
    /// </summary>
    public string Password { get; set; } = string.Empty;
}
