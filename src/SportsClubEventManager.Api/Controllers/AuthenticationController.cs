using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportsClubEventManager.Application.Authentication.Commands.Login;
using SportsClubEventManager.Application.Authentication.Commands.Logout;
using SportsClubEventManager.Application.Authentication.Commands.RefreshToken;
using SportsClubEventManager.Application.Authentication.Common;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Domain.Enums;
using SportsClubEventManager.Shared.DTOs;
using System.Security.Claims;

namespace SportsClubEventManager.Api.Controllers;

/// <summary>
/// API controller for authentication operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthenticationController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IOAuthExchangeCodeStore _oAuthExchangeCodeStore;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationController"/> class.
    /// </summary>
    /// <param name="sender">The MediatR sender for command dispatching.</param>
    /// <param name="oAuthExchangeCodeStore">Store used to hand off Google OAuth2 tokens to Web via a one-time code.</param>
    /// <param name="configuration">The application configuration.</param>
    public AuthenticationController(ISender sender, IOAuthExchangeCodeStore oAuthExchangeCodeStore, IConfiguration configuration)
    {
        _sender = sender;
        _oAuthExchangeCodeStore = oAuthExchangeCodeStore;
        _configuration = configuration;
    }

    /// <summary>
    /// Authenticates a user with local credentials.
    /// </summary>
    /// <param name="request">The login request containing email and password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A login response containing access and refresh tokens.</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var command = new LoginCommand
            {
                Email = request.Email,
                Password = request.Password
            };

            var result = await _sender.Send(command, cancellationToken);

            var response = new LoginResponse
            {
                UserId = result.UserId,
                Email = result.Email,
                Name = result.Name,
                Role = result.Role.ToString(),
                AccessToken = result.AccessToken,
                RefreshToken = result.RefreshToken,
                ExpiresIn = result.ExpiresIn
            };

            SetAuthCookies(result.AccessToken, result.RefreshToken);

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Refreshes an expired access token using a valid refresh token.
    /// </summary>
    /// <param name="request">The refresh token request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A login response containing new access and refresh tokens.</returns>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var command = new RefreshTokenCommand
            {
                RefreshToken = request.RefreshToken
            };

            var result = await _sender.Send(command, cancellationToken);

            var response = new LoginResponse
            {
                UserId = result.UserId,
                Email = result.Email,
                Name = result.Name,
                Role = result.Role.ToString(),
                AccessToken = result.AccessToken,
                RefreshToken = result.RefreshToken,
                ExpiresIn = result.ExpiresIn
            };

            SetAuthCookies(result.AccessToken, result.RefreshToken);

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Logs out the current user by revoking their refresh token.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content if successful.</returns>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var command = new LogoutCommand { UserId = userId };
        await _sender.Send(command, cancellationToken);

        ClearAuthCookies();

        return NoContent();
    }

    /// <summary>
    /// Initiates Google OAuth2 authentication flow.
    /// </summary>
    /// <returns>A challenge result redirecting to Google OAuth2.</returns>
    [HttpGet("google")]
    [AllowAnonymous]
    public IActionResult GoogleLogin()
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(GoogleCallback)),
            Items = { { "scheme", GoogleDefaults.AuthenticationScheme } }
        };

        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Handles the Google OAuth2 callback after successful authentication. Rather than establishing
    /// a session directly (the browser is talking to the Api's origin here, not Web's), it mints a
    /// one-time exchange code and redirects to Web's own callback page, which redeems it
    /// server-to-server (see <see cref="OAuthExchange"/>) — the same "authorization code" hand-off
    /// pattern OAuth2 itself uses, so the real tokens never appear in a URL (issue #125).
    /// </summary>
    /// <returns>A redirect to the web application's OAuth callback page with a one-time exchange code.</returns>
    [HttpGet("google/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleCallback()
    {
        // The Google handler (Program.cs, options.SignInScheme) persists its ticket into the
        // Cookie scheme, not its own "Google" scheme — RemoteAuthenticationHandler-derived
        // handlers like Google can't be re-authenticated by name on a later request the way
        // Cookie/JwtBearer can, since the actual OAuth2 code exchange already happened on the
        // previous request (/signin-google) and isn't repeatable here.
        var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (!authenticateResult.Succeeded)
        {
            return Redirect($"{GetWebAppBaseUrl()}/login?error=oauth_failed");
        }

        var principal = authenticateResult.Principal;
        var accessToken = principal?.FindFirstValue("access_token");
        var refreshToken = principal?.FindFirstValue("refresh_token");
        var hasUserId = Guid.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);
        var hasRole = Enum.TryParse<Role>(principal?.FindFirstValue(ClaimTypes.Role), out var role);

        // The Cookie scheme was only a temporary carrier for the Google ticket; the app's real
        // session lives in Web's own cookie once it redeems the exchange code below, so this one
        // is cleared once read rather than left behind unused.
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken) || !hasUserId || !hasRole)
        {
            return Redirect($"{GetWebAppBaseUrl()}/login?error=token_missing");
        }

        var expiresIn = _configuration.GetValue<int>("Authentication:JwtSettings:AccessTokenExpirationMinutes", 30) * 60;

        var authenticationResult = new AuthenticationResult
        {
            UserId = userId,
            Email = principal?.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            Name = principal?.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
            Role = role,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = expiresIn
        };

        var code = _oAuthExchangeCodeStore.CreateCode(authenticationResult);

        return Redirect($"{GetWebAppBaseUrl()}/oauth-callback?code={Uri.EscapeDataString(code)}");
    }

    /// <summary>
    /// Redeems a one-time OAuth2 exchange code (see <see cref="GoogleCallback"/>) for the real
    /// tokens. Called server-to-server by Web's <c>/oauth-callback</c> page, exactly like
    /// <see cref="Login"/> is called by local login — so Web can build its own session the same way
    /// for both.
    /// </summary>
    /// <param name="request">The exchange request containing the one-time code.</param>
    /// <returns>A login response containing access and refresh tokens.</returns>
    [HttpPost("oauth-exchange")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult OAuthExchange([FromBody] OAuthExchangeRequest request)
    {
        var result = _oAuthExchangeCodeStore.ConsumeCode(request.Code);

        if (result is null)
        {
            return Unauthorized(new { message = "Invalid or expired exchange code." });
        }

        var response = new LoginResponse
        {
            UserId = result.UserId,
            Email = result.Email,
            Name = result.Name,
            Role = result.Role.ToString(),
            AccessToken = result.AccessToken,
            RefreshToken = result.RefreshToken,
            ExpiresIn = result.ExpiresIn
        };

        return Ok(response);
    }

    /// <summary>
    /// Sets secure authentication cookies for access and refresh tokens.
    /// </summary>
    /// <param name="accessToken">The JWT access token.</param>
    /// <param name="refreshToken">The refresh token.</param>
    private void SetAuthCookies(string accessToken, string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddMinutes(30)
        };

        Response.Cookies.Append("access_token", accessToken, cookieOptions);

        var refreshCookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        };

        Response.Cookies.Append("refresh_token", refreshToken, refreshCookieOptions);
    }

    /// <summary>
    /// Clears authentication cookies on logout.
    /// </summary>
    private void ClearAuthCookies()
    {
        Response.Cookies.Delete("access_token");
        Response.Cookies.Delete("refresh_token");
    }

    /// <summary>
    /// Gets the base URL of the web application from configuration.
    /// </summary>
    /// <returns>The web application base URL.</returns>
    private string GetWebAppBaseUrl()
    {
        return _configuration.GetValue<string>("WebAppBaseUrl") ?? "https://localhost:5001";
    }
}
