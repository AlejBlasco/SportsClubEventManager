namespace SportsClubEventManager.Application.Authorization.Policies;

/// <summary>
/// Defines authorization policy names used throughout the application.
/// Centralizes policy names to avoid magic strings and ensure consistency.
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>
    /// Policy requiring an authenticated user (any role).
    /// </summary>
    public const string RequireAuthenticatedUser = "RequireAuthenticatedUser";

    /// <summary>
    /// Policy requiring the Administrator role.
    /// </summary>
    public const string RequireAdministratorRole = "RequireAdministratorRole";
}
