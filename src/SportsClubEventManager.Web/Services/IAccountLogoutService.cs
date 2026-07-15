namespace SportsClubEventManager.Web.Services;

/// <summary>
/// Service that revokes the signed-in user's refresh token on the Api when they log out of Web.
/// </summary>
public interface IAccountLogoutService
{
    /// <summary>
    /// Calls the Api to revoke the current user's refresh token.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogoutAsync(CancellationToken cancellationToken = default);
}
