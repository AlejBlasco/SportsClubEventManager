using System.Net.Http.Headers;

namespace SportsClubEventManager.Web.Services;

/// <summary>
/// Attaches the signed-in user's Api JWT access token to outgoing requests made by the Web app's typed HttpClients.
/// </summary>
public sealed class AuthTokenHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    /// <summary>
    /// The claim type under which the Api's JWT access token is stored in the Web app's authentication cookie.
    /// </summary>
    public const string AccessTokenClaimType = "access_token";

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var accessToken = httpContextAccessor.HttpContext?.User.FindFirst(AccessTokenClaimType)?.Value;

        if (!string.IsNullOrEmpty(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
