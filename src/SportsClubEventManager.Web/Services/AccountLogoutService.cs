namespace SportsClubEventManager.Web.Services;

/// <summary>
/// Service for revoking the signed-in user's refresh token via the Api, using HttpClient.
/// </summary>
public sealed class AccountLogoutService(HttpClient httpClient) : IAccountLogoutService
{
    /// <summary>
    /// Calls <c>POST /api/authentication/logout</c> to revoke the current user's refresh token.
    /// The caller is identified server-side from the Bearer access_token that <see cref="AuthTokenHandler"/>
    /// already attaches to this typed client, so no request body is needed.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsync("api/authentication/logout", content: null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
