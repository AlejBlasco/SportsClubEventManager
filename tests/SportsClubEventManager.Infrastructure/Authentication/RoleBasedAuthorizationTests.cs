using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Xunit;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Domain.Enums;
using SportsClubEventManager.Infrastructure.Authentication;

namespace SportsClubEventManager.UnitTests.Authorization;

/// <summary>
/// Unit tests for role-based authorization and JWT role claim generation.
/// </summary>
public sealed class RoleBasedAuthorizationTests
{
    private readonly IConfiguration _configuration;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly TokenService _tokenService;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoleBasedAuthorizationTests"/> class.
    /// </summary>
    public RoleBasedAuthorizationTests()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Authentication:JwtSettings:SecretKey", "ThisIsAVerySecureSecretKeyForTestingPurposes12345678"},
            {"Authentication:JwtSettings:Issuer", "SportsClubEventManager.Api"},
            {"Authentication:JwtSettings:Audience", "SportsClubEventManager.Web"},
            {"Authentication:JwtSettings:AccessTokenExpirationMinutes", "30"}
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _dateTimeProvider.UtcNow.Returns(callInfo => DateTime.UtcNow);

        _tokenService = new TokenService(_configuration, _dateTimeProvider);
    }

    /// <summary>
    /// Verifies that TokenService includes User role claim in JWT token.
    /// </summary>
    [Fact]
    public void GenerateAccessToken_WithUserRole_IncludesRoleClaimInToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "user@example.com";
        var name = "Standard User";
        var role = Role.User;

        // Act
        var token = _tokenService.GenerateAccessToken(userId, email, name, role);

        // Assert
        token.Should().NotBeNullOrEmpty();
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Claims.Should().Contain(c =>
            c.Type == System.Security.Claims.ClaimTypes.Role &&
            c.Value == "User");
    }

    /// <summary>
    /// Verifies that TokenService includes Administrator role claim in JWT token.
    /// </summary>
    [Fact]
    public void GenerateAccessToken_WithAdministratorRole_IncludesRoleClaimInToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "admin@example.com";
        var name = "System Administrator";
        var role = Role.Administrator;

        // Act
        var token = _tokenService.GenerateAccessToken(userId, email, name, role);

        // Assert
        token.Should().NotBeNullOrEmpty();
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Claims.Should().Contain(c =>
            c.Type == System.Security.Claims.ClaimTypes.Role &&
            c.Value == "Administrator");
    }

    /// <summary>
    /// Verifies that TokenService includes all required claims plus role claim in token.
    /// </summary>
    [Fact]
    public void GenerateAccessToken_WithValidInputs_IncludesAllRequiredClaimsIncludingRole()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "complete@example.com";
        var name = "Complete Test User";
        var role = Role.Administrator;

        // Act
        var token = _tokenService.GenerateAccessToken(userId, email, name, role);

        // Assert
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Claims.Should()
            .Contain(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub && c.Value == userId.ToString())
            .And.Contain(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email && c.Value == email)
            .And.Contain(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Name && c.Value == name)
            .And.Contain(c => c.Type == System.Security.Claims.ClaimTypes.Role && c.Value == "Administrator");
    }

    /// <summary>
    /// Verifies that role claim is correctly encoded as string representation of Role enum.
    /// </summary>
    [Fact]
    public void GenerateAccessToken_RoleClaimValue_MatchesRoleEnumStringValue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var name = "Test User";
        var role = Role.User;

        // Act
        var token = _tokenService.GenerateAccessToken(userId, email, name, role);

        // Assert
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var roleClaim = jwtToken.Claims.FirstOrDefault(c =>
            c.Type == System.Security.Claims.ClaimTypes.Role);

        roleClaim.Should().NotBeNull();
        roleClaim!.Value.Should().Be(role.ToString());
    }

    /// <summary>
    /// Verifies that different roles produce different role claim values in tokens.
    /// </summary>
    [Fact]
    public void GenerateAccessToken_DifferentRoles_ProduceDifferentRoleClaimValues()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var name = "Test User";

        // Act
        var userToken = _tokenService.GenerateAccessToken(userId, email, name, Role.User);
        var adminToken = _tokenService.GenerateAccessToken(userId, email, name, Role.Administrator);

        // Assert
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var userJwt = handler.ReadJwtToken(userToken);
        var adminJwt = handler.ReadJwtToken(adminToken);

        var userRoleClaim = userJwt.Claims.First(c =>
            c.Type == System.Security.Claims.ClaimTypes.Role);
        var adminRoleClaim = adminJwt.Claims.First(c =>
            c.Type == System.Security.Claims.ClaimTypes.Role);

        userRoleClaim.Value.Should().Be("User");
        adminRoleClaim.Value.Should().Be("Administrator");
        userRoleClaim.Value.Should().NotBe(adminRoleClaim.Value);
    }

    /// <summary>
    /// Verifies that CancellationToken parameter is optional when calling GenerateAccessToken.
    /// </summary>
    [Fact]
    public void GenerateAccessToken_WithoutCancellationToken_UsesDefaultValue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var name = "Test User";
        var role = Role.User;

        // Act
        // Call without CancellationToken parameter to verify default value works
        var token = _tokenService.GenerateAccessToken(userId, email, name, role);

        // Assert
        token.Should().NotBeNullOrEmpty();
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        jwtToken.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that CancellationToken parameter is accepted when calling GenerateAccessToken.
    /// </summary>
    [Fact]
    public void GenerateAccessToken_WithCancellationToken_ProducesValidToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var name = "Test User";
        var role = Role.Administrator;
        using var cts = new CancellationTokenSource();

        // Act
        var token = _tokenService.GenerateAccessToken(userId, email, name, role, cts.Token);

        // Assert
        token.Should().NotBeNullOrEmpty();
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        jwtToken.Claims.Should().Contain(c =>
            c.Type == System.Security.Claims.ClaimTypes.Role &&
            c.Value == "Administrator");
    }

    /// <summary>
    /// Verifies that GenerateRefreshToken also accepts optional CancellationToken parameter.
    /// </summary>
    [Fact]
    public void GenerateRefreshToken_WithCancellationToken_ProducesValidToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        var refreshToken = _tokenService.GenerateRefreshToken(cts.Token);

        // Assert
        refreshToken.Should().NotBeNullOrEmpty();
        refreshToken.Length.Should().BeGreaterThan(40);
    }

    /// <summary>
    /// Verifies that ValidateAccessToken also accepts optional CancellationToken parameter.
    /// </summary>
    [Fact]
    public void ValidateAccessToken_WithCancellationToken_HandlesInvalidToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        var result = _tokenService.ValidateAccessToken("invalid.token.here", cts.Token);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that HashRefreshToken also accepts optional CancellationToken parameter.
    /// </summary>
    [Fact]
    public void HashRefreshToken_WithCancellationToken_ProducesConsistentHash()
    {
        // Arrange
        var refreshToken = "test_refresh_token";
        using var cts = new CancellationTokenSource();

        // Act
        var hash1 = _tokenService.HashRefreshToken(refreshToken, cts.Token);
        var hash2 = _tokenService.HashRefreshToken(refreshToken, cts.Token);

        // Assert
        hash1.Should().Be(hash2);
    }
}
