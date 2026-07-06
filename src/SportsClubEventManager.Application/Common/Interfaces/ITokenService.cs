using SportsClubEventManager.Domain.Enums;

namespace SportsClubEventManager.Application.Common.Interfaces;

/// <summary>
/// Interface for JWT token generation and validation.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates a JWT access token for the specified user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="email">The email address of the user.</param>
    /// <param name="name">The name of the user.</param>
    /// <param name="role">The role of the user for authorization.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A JWT access token string.</returns>
    string GenerateAccessToken(Guid userId, string email, string name, Role role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a secure refresh token.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A cryptographically secure refresh token string.</returns>
    string GenerateRefreshToken(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a JWT access token and extracts the user ID.
    /// </summary>
    /// <param name="token">The JWT token to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user ID if validation succeeds; otherwise, null.</returns>
    Guid? ValidateAccessToken(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Hashes a refresh token for secure storage.
    /// </summary>
    /// <param name="refreshToken">The plain-text refresh token to hash.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The hashed refresh token.</returns>
    string HashRefreshToken(string refreshToken, CancellationToken cancellationToken = default);
}
