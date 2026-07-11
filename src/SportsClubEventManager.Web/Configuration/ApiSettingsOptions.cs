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
    /// Blazor Server app calls.
    /// </summary>
    [Required, Url]
    public string BaseUrl { get; init; } = string.Empty;
}
