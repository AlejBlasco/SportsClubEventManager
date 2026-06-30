namespace SportsClubEventManager.Shared.DTOs;

/// <summary>
/// Data transfer object for refresh token requests.
/// </summary>
public sealed class RefreshTokenRequest
{
    /// <summary>
    /// Gets or sets the refresh token to validate and use for generating new tokens.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;
}
