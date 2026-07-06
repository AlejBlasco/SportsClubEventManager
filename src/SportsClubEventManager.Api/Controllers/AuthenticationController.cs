using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportsClubEventManager.Application.Authentication.Commands.Login;
using SportsClubEventManager.Application.Authentication.Commands.Logout;
using SportsClubEventManager.Application.Authentication.Commands.RefreshToken;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationController"/> class.
    /// </summary>
    /// <param name="sender">The MediatR sender for command dispatching.</param>
    public AuthenticationController(ISender sender)
    {
        _sender = sender;
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
    /// Handles the Google OAuth2 callback after successful authentication.
    /// </summary>
    /// <returns>A redirect to the web application with authentication tokens.</returns>
    [HttpGet("google/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleCallback()
    {
        var authenticateResult = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

        if (!authenticateResult.Succeeded)
        {
            return Redirect($"{GetWebAppBaseUrl()}/login?error=oauth_failed");
        }

        var accessToken = authenticateResult.Principal?.FindFirstValue("access_token");
        var refreshToken = authenticateResult.Principal?.FindFirstValue("refresh_token");

        if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
        {
            return Redirect($"{GetWebAppBaseUrl()}/login?error=token_missing");
        }

        SetAuthCookies(accessToken, refreshToken);

        return Redirect($"{GetWebAppBaseUrl()}/");
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
        return HttpContext.RequestServices.GetRequiredService<IConfiguration>()
            .GetValue<string>("WebAppBaseUrl") ?? "https://localhost:5001";
    }
}
