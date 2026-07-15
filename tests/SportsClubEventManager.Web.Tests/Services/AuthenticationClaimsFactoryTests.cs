using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication.Cookies;
using SportsClubEventManager.Shared.DTOs;
using SportsClubEventManager.Web.Services;
using Xunit;

namespace SportsClubEventManager.Web.Tests.Services;

/// <summary>
/// Unit tests for <see cref="AuthenticationClaimsFactory"/>, the single place that builds Web's own
/// session claims for both local login and the Google OAuth2 callback (issue #125) — previously this
/// logic was duplicated inline in Login.razor and never replicated for Google, which was the root
/// cause of the bug.
/// </summary>
public sealed class AuthenticationClaimsFactoryTests
{
    private static LoginResponse CreateLoginResponse() => new()
    {
        UserId = Guid.NewGuid(),
        Email = "user@example.com",
        Name = "Test User",
        Role = "Administrator",
        AccessToken = "the-access-token",
        RefreshToken = "the-refresh-token",
        ExpiresIn = 1800
    };

    /// <summary>
    /// Verifies that every field of the login response is mapped to its corresponding claim.
    /// </summary>
    [Fact]
    public void Create_WithLoginResponse_MapsAllFieldsToClaims()
    {
        // Arrange
        var loginResponse = CreateLoginResponse();

        // Act
        var principal = AuthenticationClaimsFactory.Create(loginResponse);

        // Assert
        principal.FindFirstValue(ClaimTypes.NameIdentifier).Should().Be(loginResponse.UserId.ToString());
        principal.FindFirstValue(ClaimTypes.Name).Should().Be(loginResponse.Name);
        principal.FindFirstValue(ClaimTypes.Email).Should().Be(loginResponse.Email);
        principal.FindFirstValue(ClaimTypes.Role).Should().Be(loginResponse.Role);
        principal.FindFirstValue(AuthTokenHandler.AccessTokenClaimType).Should().Be(loginResponse.AccessToken);
    }

    /// <summary>
    /// Verifies that the produced identity is authenticated under Web's own cookie scheme, so
    /// <c>HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal)</c>
    /// establishes a real session regardless of which login path (local or Google) produced it.
    /// </summary>
    [Fact]
    public void Create_WithLoginResponse_UsesCookieAuthenticationScheme()
    {
        // Arrange
        var loginResponse = CreateLoginResponse();

        // Act
        var principal = AuthenticationClaimsFactory.Create(loginResponse);

        // Assert
        principal.Identity.Should().NotBeNull();
        principal.Identity!.IsAuthenticated.Should().BeTrue();
        principal.Identity.AuthenticationType.Should().Be(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
