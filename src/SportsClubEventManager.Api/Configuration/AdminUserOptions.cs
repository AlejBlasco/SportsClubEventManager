namespace SportsClubEventManager.Api.Configuration;

/// <summary>
/// Strongly typed representation of the "AdminUser" configuration section. Only the
/// seeded administrator password is modeled here; the validation rule for this value is
/// conditional on the hosting environment and is enforced by <see cref="AdminUserOptionsValidator"/>,
/// not by data annotations.
/// </summary>
public sealed class AdminUserOptions
{
    /// <summary>
    /// The configuration section name this options class binds to.
    /// </summary>
    public const string SectionName = "AdminUser";

    /// <summary>
    /// Gets the password used to seed the default administrator account
    /// (<c>admin@sportsclub.local</c>). Required outside the Development environment.
    /// </summary>
    public string? Password { get; init; }
}
