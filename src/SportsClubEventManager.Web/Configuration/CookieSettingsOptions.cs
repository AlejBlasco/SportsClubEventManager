namespace SportsClubEventManager.Web.Configuration;

/// <summary>
/// Strongly typed representation of the "Authentication:CookieSettings" configuration section.
/// The native <see cref="IConfiguration"/> binder converts a value such as <c>"00:30:00"</c>
/// into <see cref="TimeSpan"/> automatically, so no manual parsing is needed.
/// </summary>
public sealed class CookieSettingsOptions
{
    /// <summary>
    /// The configuration section name this options class binds to.
    /// </summary>
    public const string SectionName = "Authentication:CookieSettings";

    /// <summary>
    /// Gets the name of the authentication cookie.
    /// </summary>
    public string CookieName { get; init; } = ".SportsClubEventManager.Auth";

    /// <summary>
    /// Gets the path the user is redirected to when a login is required.
    /// </summary>
    public string LoginPath { get; init; } = "/login";

    /// <summary>
    /// Gets the path the user is redirected to after logging out.
    /// </summary>
    public string LogoutPath { get; init; } = "/logout";

    /// <summary>
    /// Gets the path the user is redirected to when access is denied.
    /// </summary>
    public string AccessDeniedPath { get; init; } = "/access-denied";

    /// <summary>
    /// Gets the lifetime of the authentication cookie.
    /// </summary>
    public TimeSpan ExpireTimeSpan { get; init; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Gets a value indicating whether the cookie expiration slides forward on activity.
    /// </summary>
    public bool SlidingExpiration { get; init; } = true;
}
