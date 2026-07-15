using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Web.Services;

/// <summary>
/// Builds the Web app's own <see cref="ClaimsPrincipal"/> from an Api <see cref="LoginResponse"/>.
/// Used identically by local login and the OAuth2 callback page so both establish the same session
/// shape — this single place is what issue #125 was missing for the Google flow.
/// </summary>
public static class AuthenticationClaimsFactory
{
    /// <summary>
    /// Creates the <see cref="ClaimsPrincipal"/> for Web's own cookie authentication scheme from an
    /// Api login result.
    /// </summary>
    /// <param name="loginResult">The login result returned by the Api.</param>
    /// <returns>A <see cref="ClaimsPrincipal"/> ready to be passed to <c>HttpContext.SignInAsync</c>.</returns>
    public static ClaimsPrincipal Create(LoginResponse loginResult)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, loginResult.UserId.ToString()),
            new Claim(ClaimTypes.Name, loginResult.Name),
            new Claim(ClaimTypes.Email, loginResult.Email),
            new Claim(ClaimTypes.Role, loginResult.Role),
            new Claim(AuthTokenHandler.AccessTokenClaimType, loginResult.AccessToken),
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }
}
