using System.ComponentModel.DataAnnotations;

namespace SportsClubEventManager.Api.Configuration;

/// <summary>
/// Strongly typed representation of the "Authentication:Google" configuration section,
/// validated at startup via <c>ValidateDataAnnotations().ValidateOnStart()</c>.
/// </summary>
public sealed class GoogleAuthOptions
{
    /// <summary>
    /// The configuration section name this options class binds to.
    /// </summary>
    public const string SectionName = "Authentication:Google";

    /// <summary>
    /// Gets the OAuth2 client ID registered in Google Cloud Console.
    /// </summary>
    [Required]
    public string ClientId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the OAuth2 client secret registered in Google Cloud Console.
    /// </summary>
    [Required]
    public string ClientSecret { get; init; } = string.Empty;

    /// <summary>
    /// Gets the callback path the Google OAuth2 middleware listens on.
    /// </summary>
    public string CallbackPath { get; init; } = "/signin-google";
}
