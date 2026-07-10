namespace SportsClubEventManager.Api.Configuration;

/// <summary>
/// Strongly typed representation of the "Cors" configuration section. The environment-conditional
/// validation rule (at least one origin required outside Development) is enforced by
/// <see cref="CorsOptionsValidator"/>, not by data annotations.
/// </summary>
public sealed class CorsOptions
{
    /// <summary>
    /// The configuration section name this options class binds to.
    /// </summary>
    public const string SectionName = "Cors";

    /// <summary>
    /// Gets the list of origins allowed to make cross-origin requests to the API.
    /// </summary>
    public string[] AllowedOrigins { get; init; } = [];
}
