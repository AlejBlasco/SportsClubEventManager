using System.Security.Claims;
using Bunit;
using Bunit.TestDoubles;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;
using SportsClubEventManager.Web.Components.Authentication;

namespace SportsClubEventManager.Web.Components.Authentication;

/// <summary>
/// bUnit tests for the LoginDisplay component.
/// </summary>
public sealed class LoginDisplayTests : TestContext
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoginDisplayTests"/> class.
    /// </summary>
    public LoginDisplayTests()
    {
        _configuration = Substitute.For<IConfiguration>();
        _configuration["ApiSettings:BaseUrl"].Returns("https://localhost:5001");

        _httpClient = Substitute.For<HttpClient>();

        Services.AddSingleton(_configuration);
        Services.AddSingleton(_httpClient);
    }

    /// <summary>
    /// Verifies that login link is shown when user is not authenticated.
    /// </summary>
    [Fact]
    public void Render_WhenNotAuthenticated_ShowsLoginLink()
    {
        // Arrange
        var authContext = this.AddTestAuthorization();
        authContext.SetNotAuthorized();

        // Act
        var cut = RenderComponent<LoginDisplay>();

        // Assert
        cut.Find("a[href='/login']").Should().NotBeNull();
        cut.Find("a[href='/login']").TextContent.Should().Contain("Login");
    }

    /// <summary>
    /// Verifies that user name and logout link are shown when authenticated.
    /// </summary>
    [Fact]
    public void Render_WhenAuthenticated_ShowsUserNameAndLogoutLink()
    {
        // Arrange
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("Test User");
        authContext.SetClaims(
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.Email, "test@example.com")
        );

        // Act
        var cut = RenderComponent<LoginDisplay>();

        // Assert
        cut.Find(".user-name").TextContent.Should().Contain("Hello, Test User");
        cut.Find("a[href='/account/logout']").TextContent.Should().Contain("Logout");
    }

    /// <summary>
    /// Verifies that component renders without errors when user has no name claim.
    /// </summary>
    [Fact]
    public void Render_WhenAuthenticatedWithoutNameClaim_DoesNotThrow()
    {
        // Arrange
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("test@example.com");
        authContext.SetClaims(
            new Claim(ClaimTypes.Email, "test@example.com")
        );

        // Act
        var act = () => RenderComponent<LoginDisplay>();

        // Assert
        act.Should().NotThrow();
    }
}
