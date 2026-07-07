namespace SportsClubEventManager.Shared.DTOs;

/// <summary>
/// Response model for password change operation.
/// </summary>
public sealed record ChangePasswordResponse
{
    /// <summary>
    /// Gets the success message.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Gets the new access token.
    /// </summary>
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>
    /// Gets the new refresh token.
    /// </summary>
    public string RefreshToken { get; init; } = string.Empty;

    /// <summary>
    /// Gets the token expiration time in seconds.
    /// </summary>
    public int ExpiresIn { get; init; }
}
