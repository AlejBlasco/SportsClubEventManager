using SportsClubEventManager.Domain.Enums;

namespace SportsClubEventManager.Application.Authentication.Common;

/// <summary>
/// Represents the result of an authentication operation.
/// </summary>
public sealed class AuthenticationResult
{
    /// <summary>
    /// Gets the unique identifier of the authenticated user.
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// Gets the email address of the authenticated user.
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Gets the name of the authenticated user.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the role of the authenticated user for authorization.
    /// </summary>
    public Role Role { get; init; }

    /// <summary>
    /// Gets the JWT access token.
    /// </summary>
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>
    /// Gets the refresh token for session renewal.
    /// </summary>
    public string RefreshToken { get; init; } = string.Empty;

    /// <summary>
    /// Gets the expiration time of the access token in seconds.
    /// </summary>
    public int ExpiresIn { get; init; }
}
