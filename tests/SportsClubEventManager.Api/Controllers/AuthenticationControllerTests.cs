using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SportsClubEventManager.Api.Controllers;
using SportsClubEventManager.Application.Authentication.Common;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Domain.Enums;
using SportsClubEventManager.Shared.DTOs;
using Xunit;

namespace SportsClubEventManager.Api.Controllers;

/// <summary>
/// Unit tests for the Google OAuth2 hand-off between <see cref="AuthenticationController.GoogleCallback"/>
/// and <see cref="AuthenticationController.OAuthExchange"/> (issue #125), covering the exchange-code
/// creation/redemption logic that lives in the controller because it has no domain/database concern of
/// its own (it wraps <see cref="IOAuthExchangeCodeStore"/>, the same way <c>SetAuthCookies</c> already does
/// for cookie plumbing).
/// </summary>
public sealed class AuthenticationControllerTests
{
    private readonly IOAuthExchangeCodeStore _codeStore = Substitute.For<IOAuthExchangeCodeStore>();
    private readonly IAuthenticationService _authenticationService = Substitute.For<IAuthenticationService>();
    private readonly AuthenticationController _sut;
    private readonly DefaultHttpContext _httpContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationControllerTests"/> class.
    /// </summary>
    public AuthenticationControllerTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "WebAppBaseUrl", "https://web.example.com" },
                { "Authentication:JwtSettings:AccessTokenExpirationMinutes", "30" }
            })
            .Build();

        _authenticationService.SignOutAsync(Arg.Any<HttpContext>(), Arg.Any<string?>(), Arg.Any<AuthenticationProperties?>())
            .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddSingleton(_authenticationService);
        var serviceProvider = services.BuildServiceProvider();

        _httpContext = new DefaultHttpContext { RequestServices = serviceProvider };

        _sut = new AuthenticationController(Substitute.For<ISender>(), _codeStore, configuration)
        {
            ControllerContext = new ControllerContext { HttpContext = _httpContext }
        };
    }

    private void SetAuthenticateResult(AuthenticateResult result)
    {
        _authenticationService.AuthenticateAsync(_httpContext, Arg.Any<string?>()).Returns(result);
    }

    private static ClaimsPrincipal CreateGooglePrincipal(
        Guid userId,
        string email = "user@example.com",
        string name = "Test User",
        string role = "User",
        string accessToken = "access-token",
        string refreshToken = "refresh-token")
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, name),
            new(ClaimTypes.Role, role),
            new("access_token", accessToken),
            new("refresh_token", refreshToken)
        };

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Cookies"));
    }

    /// <summary>
    /// Verifies that when the temporary Cookie-scheme authentication fails (e.g. Google denied consent),
    /// the callback redirects to Web's login page with an "oauth_failed" error, never reaching the
    /// exchange-code store.
    /// </summary>
    [Fact]
    public async Task GoogleCallback_WhenAuthenticationFails_RedirectsToLoginWithOAuthFailedError()
    {
        // Arrange
        SetAuthenticateResult(AuthenticateResult.Fail("no ticket"));

        // Act
        var result = await _sut.GoogleCallback();

        // Assert
        var redirect = result.Should().BeOfType<RedirectResult>().Subject;
        redirect.Url.Should().Be("https://web.example.com/login?error=oauth_failed");
        _codeStore.DidNotReceive().CreateCode(Arg.Any<AuthenticationResult>());
    }

    /// <summary>
    /// Verifies that when the Google ticket is missing the access/refresh token claims, the callback
    /// redirects with a "token_missing" error instead of creating an exchange code.
    /// </summary>
    [Fact]
    public async Task GoogleCallback_WhenTokenClaimsMissing_RedirectsToLoginWithTokenMissingError()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())], "Cookies"));
        SetAuthenticateResult(AuthenticateResult.Success(new AuthenticationTicket(principal, "Cookies")));

        // Act
        var result = await _sut.GoogleCallback();

        // Assert
        var redirect = result.Should().BeOfType<RedirectResult>().Subject;
        redirect.Url.Should().Be("https://web.example.com/login?error=token_missing");
        _codeStore.DidNotReceive().CreateCode(Arg.Any<AuthenticationResult>());
    }

    /// <summary>
    /// Verifies that when the Google role claim cannot be parsed into the <see cref="Role"/> enum, the
    /// callback treats the ticket as invalid rather than defaulting to a role.
    /// </summary>
    [Fact]
    public async Task GoogleCallback_WhenRoleClaimInvalid_RedirectsToLoginWithTokenMissingError()
    {
        // Arrange
        var principal = CreateGooglePrincipal(Guid.NewGuid(), role: "NotARealRole");
        SetAuthenticateResult(AuthenticateResult.Success(new AuthenticationTicket(principal, "Cookies")));

        // Act
        var result = await _sut.GoogleCallback();

        // Assert
        var redirect = result.Should().BeOfType<RedirectResult>().Subject;
        redirect.Url.Should().Be("https://web.example.com/login?error=token_missing");
    }

    /// <summary>
    /// Verifies the happy path: a valid Google ticket produces an <see cref="AuthenticationResult"/>
    /// (with ExpiresIn computed from configuration) handed to the code store, and redirects the browser
    /// to Web's oauth-callback page with the resulting one-time code — never with the tokens themselves.
    /// </summary>
    [Fact]
    public async Task GoogleCallback_WhenTicketValid_CreatesCodeAndRedirectsToWebOAuthCallback()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var principal = CreateGooglePrincipal(userId, email: "user@example.com", name: "Test User", role: "Administrator",
            accessToken: "the-access-token", refreshToken: "the-refresh-token");
        SetAuthenticateResult(AuthenticateResult.Success(new AuthenticationTicket(principal, "Cookies")));
        _codeStore.CreateCode(Arg.Any<AuthenticationResult>()).Returns("one-time-code");

        // Act
        var result = await _sut.GoogleCallback();

        // Assert
        var redirect = result.Should().BeOfType<RedirectResult>().Subject;
        redirect.Url.Should().Be("https://web.example.com/oauth-callback?code=one-time-code");

        _codeStore.Received(1).CreateCode(Arg.Is<AuthenticationResult>(r =>
            r.UserId == userId &&
            r.Email == "user@example.com" &&
            r.Name == "Test User" &&
            r.Role == Role.Administrator &&
            r.AccessToken == "the-access-token" &&
            r.RefreshToken == "the-refresh-token" &&
            r.ExpiresIn == 1800));
    }

    /// <summary>
    /// Verifies that the temporary Cookie-scheme ticket used to carry the Google result is always signed
    /// out once read, regardless of whether the overall exchange succeeds.
    /// </summary>
    [Fact]
    public async Task GoogleCallback_WhenTicketValid_SignsOutTemporaryCookieScheme()
    {
        // Arrange
        var principal = CreateGooglePrincipal(Guid.NewGuid());
        SetAuthenticateResult(AuthenticateResult.Success(new AuthenticationTicket(principal, "Cookies")));

        // Act
        await _sut.GoogleCallback();

        // Assert
        await _authenticationService.Received(1).SignOutAsync(_httpContext, Arg.Any<string?>(), Arg.Any<AuthenticationProperties?>());
    }

    /// <summary>
    /// Verifies that redeeming a valid, previously-created code returns the associated tokens as a
    /// <see cref="LoginResponse"/> — the same shape local login returns from <c>/api/authentication/login</c>.
    /// </summary>
    [Fact]
    public void OAuthExchange_WhenCodeIsValid_ReturnsOkWithLoginResponse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var authenticationResult = new AuthenticationResult
        {
            UserId = userId,
            Email = "user@example.com",
            Name = "Test User",
            Role = Role.User,
            AccessToken = "access-token",
            RefreshToken = "refresh-token",
            ExpiresIn = 1800
        };
        _codeStore.ConsumeCode("valid-code").Returns(authenticationResult);

        // Act
        var result = _sut.OAuthExchange(new OAuthExchangeRequest { Code = "valid-code" });

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<LoginResponse>().Subject;
        response.UserId.Should().Be(userId);
        response.Role.Should().Be("User");
        response.AccessToken.Should().Be("access-token");
        response.RefreshToken.Should().Be("refresh-token");
        response.ExpiresIn.Should().Be(1800);
    }

    /// <summary>
    /// Verifies that redeeming an unknown, expired, or already-consumed code returns 401 rather than
    /// throwing, since the code store returns null for all three cases.
    /// </summary>
    [Fact]
    public void OAuthExchange_WhenCodeIsInvalidOrExpired_ReturnsUnauthorized()
    {
        // Arrange
        _codeStore.ConsumeCode("bad-code").Returns((AuthenticationResult?)null);

        // Act
        var result = _sut.OAuthExchange(new OAuthExchangeRequest { Code = "bad-code" });

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }
}
