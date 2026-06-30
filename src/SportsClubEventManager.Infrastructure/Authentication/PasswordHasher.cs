using SportsClubEventManager.Application.Common.Interfaces;

namespace SportsClubEventManager.Infrastructure.Authentication;

/// <summary>
/// Implementation of password hashing using BCrypt algorithm with cost factor 12.
/// </summary>
public sealed class PasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    /// <summary>
    /// Hashes a plain-text password using BCrypt with cost factor 12.
    /// </summary>
    /// <param name="password">The plain-text password to hash.</param>
    /// <returns>The hashed password string including salt.</returns>
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    /// <summary>
    /// Verifies that a plain-text password matches a BCrypt hashed password.
    /// </summary>
    /// <param name="password">The plain-text password to verify.</param>
    /// <param name="hashedPassword">The BCrypt hashed password to compare against.</param>
    /// <returns>True if the password matches; otherwise, false.</returns>
    public bool VerifyPassword(string password, string hashedPassword)
    {
        return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
    }
}
