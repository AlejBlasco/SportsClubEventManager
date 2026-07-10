using System.ComponentModel.DataAnnotations;

namespace SportsClubEventManager.Api.Configuration;

/// <summary>
/// Strongly typed representation of the "Authentication:JwtSettings" configuration section,
/// validated at startup via <c>ValidateDataAnnotations().ValidateOnStart()</c>.
/// </summary>
public sealed class JwtSettingsOptions
{
    /// <summary>
    /// The configuration section name this options class binds to.
    /// </summary>
    public const string SectionName = "Authentication:JwtSettings";

    /// <summary>
    /// Gets the symmetric key used to sign and validate JWT access tokens.
    /// Must be at least 32 characters (256 bits) to satisfy HMAC-SHA256 requirements.
    /// </summary>
    [Required, MinLength(32)]
    public string SecretKey { get; init; } = string.Empty;

    /// <summary>
    /// Gets the JWT issuer ("iss" claim) expected on validated tokens.
    /// </summary>
    [Required]
    public string Issuer { get; init; } = string.Empty;

    /// <summary>
    /// Gets the JWT audience ("aud" claim) expected on validated tokens.
    /// </summary>
    [Required]
    public string Audience { get; init; } = string.Empty;

    /// <summary>
    /// Gets the lifetime, in minutes, of an issued access token.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int AccessTokenExpirationMinutes { get; init; } = 30;

    /// <summary>
    /// Gets the lifetime, in days, of an issued refresh token.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int RefreshTokenExpirationDays { get; init; } = 7;
}
