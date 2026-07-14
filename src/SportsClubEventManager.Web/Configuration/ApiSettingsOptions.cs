using System.ComponentModel.DataAnnotations;

namespace SportsClubEventManager.Web.Configuration;

/// <summary>
/// Strongly typed representation of the "ApiSettings" configuration section,
/// validated at startup via <c>ValidateDataAnnotations().ValidateOnStart()</c>.
/// </summary>
public sealed class ApiSettingsOptions
{
    /// <summary>
    /// The configuration section name this options class binds to.
    /// </summary>
    public const string SectionName = "ApiSettings";

    /// <summary>
    /// Gets the base URL of the API host that every typed <see cref="HttpClient"/> in this
    /// Blazor Server app calls. Server-to-server only — in production this is the internal
    /// Docker hostname (e.g. <c>http://api:8080</c>), unreachable from the user's browser.
    /// </summary>
    [Required, Url]
    public string BaseUrl { get; init; } = string.Empty;

    /// <summary>
    /// Gets the base URL of the API host used to build links the browser navigates to directly
    /// (e.g. the "Sign in with Google" button, which triggers a full-page redirect handled by the
    /// Api's OAuth2 middleware). Must be publicly reachable, unlike <see cref="BaseUrl"/>.
    /// </summary>
    [Required, Url]
    public string PublicBaseUrl { get; init; } = string.Empty;
}
