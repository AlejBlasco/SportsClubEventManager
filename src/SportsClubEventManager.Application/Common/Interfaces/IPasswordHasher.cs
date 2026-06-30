namespace SportsClubEventManager.Application.Common.Interfaces;

/// <summary>
/// Interface for password hashing and verification.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a plain-text password using a secure algorithm.
    /// </summary>
    /// <param name="password">The plain-text password to hash.</param>
    /// <returns>The hashed password string.</returns>
    string HashPassword(string password);

    /// <summary>
    /// Verifies that a plain-text password matches a hashed password.
    /// </summary>
    /// <param name="password">The plain-text password to verify.</param>
    /// <param name="hashedPassword">The hashed password to compare against.</param>
    /// <returns>True if the password matches; otherwise, false.</returns>
    bool VerifyPassword(string password, string hashedPassword);
}
