using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;
using SportsClubEventManager.Application.Authentication.Commands.Login;
using SportsClubEventManager.Application.Authentication.Commands.RefreshToken;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Domain.Entities;
using SportsClubEventManager.Domain.Enums;

namespace SportsClubEventManager.UnitTests.Authorization;

/// <summary>
/// Unit tests verifying role claims are included in authentication results from login and refresh token commands.
/// </summary>
public sealed class AuthenticationCommandHandlerRoleTests
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;
    private readonly IDateTimeProvider _dateTimeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationCommandHandlerRoleTests"/> class.
    /// </summary>
    public AuthenticationCommandHandlerRoleTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
        _passwordHasher = Substitute.For<IPasswordHasher>();
        _tokenService = Substitute.For<ITokenService>();

        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Authentication:JwtSettings:AccessTokenExpirationMinutes", "30"},
            {"Authentication:JwtSettings:RefreshTokenExpirationDays", "7"}
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _dateTimeProvider.UtcNow.Returns(new DateTime(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc));
    }

    /// <summary>
    /// Verifies that LoginCommandHandler includes User role in authentication result.
    /// </summary>
    [Fact]
    public async Task LoginCommandHandler_WhenUserHasUserRole_IncludesRoleInResult()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            Name = "Standard User",
            PasswordHash = "hashed_password",
            IsActive = true,
            Gender = Gender.Male,
            Role = Role.User
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        _passwordHasher.VerifyPassword("password123", "hashed_password").Returns(true);
        _tokenService.GenerateAccessToken(user.Id, user.Email, user.Name, Role.User).Returns("access_token");
        _tokenService.GenerateRefreshToken().Returns("refresh_token");
        _tokenService.HashRefreshToken("refresh_token").Returns("hashed_refresh_token");

        var handler = new LoginCommandHandler(_context, _passwordHasher, _tokenService, _configuration, _dateTimeProvider);
        var command = new LoginCommand { Email = "user@example.com", Password = "password123" };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Role.Should().Be(Role.User);
    }

    /// <summary>
    /// Verifies that LoginCommandHandler includes Administrator role in authentication result.
    /// </summary>
    [Fact]
    public async Task LoginCommandHandler_WhenUserHasAdministratorRole_IncludesRoleInResult()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@example.com",
            Name = "System Administrator",
            PasswordHash = "hashed_password",
            IsActive = true,
            Gender = Gender.Other,
            Role = Role.Administrator
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        _passwordHasher.VerifyPassword("password123", "hashed_password").Returns(true);
        _tokenService.GenerateAccessToken(user.Id, user.Email, user.Name, Role.Administrator).Returns("access_token");
        _tokenService.GenerateRefreshToken().Returns("refresh_token");
        _tokenService.HashRefreshToken("refresh_token").Returns("hashed_refresh_token");

        var handler = new LoginCommandHandler(_context, _passwordHasher, _tokenService, _configuration, _dateTimeProvider);
        var command = new LoginCommand { Email = "admin@example.com", Password = "password123" };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Role.Should().Be(Role.Administrator);
    }

    /// <summary>
    /// Verifies that LoginCommandHandler passes user role to TokenService when generating access token.
    /// </summary>
    [Fact]
    public async Task LoginCommandHandler_GeneratesAccessToken_WithUserRole()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            Name = "Test User",
            PasswordHash = "hashed_password",
            IsActive = true,
            Gender = Gender.Female,
            Role = Role.User
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        _passwordHasher.VerifyPassword("password123", "hashed_password").Returns(true);
        _tokenService.GenerateAccessToken(user.Id, user.Email, user.Name, Role.User).Returns("access_token");
        _tokenService.GenerateRefreshToken().Returns("refresh_token");
        _tokenService.HashRefreshToken("refresh_token").Returns("hashed_refresh_token");

        var handler = new LoginCommandHandler(_context, _passwordHasher, _tokenService, _configuration, _dateTimeProvider);
        var command = new LoginCommand { Email = "user@example.com", Password = "password123" };

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _tokenService.Received(1).GenerateAccessToken(
            user.Id,
            user.Email,
            user.Name,
            Role.User,
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that RefreshTokenCommandHandler includes User role in new authentication result.
    /// </summary>
    [Fact]
    public async Task RefreshTokenCommandHandler_WhenUserHasUserRole_IncludesRoleInResult()
    {
        // Arrange
        var hashedRefreshToken = "hashed_token";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            Name = "Standard User",
            IsActive = true,
            Gender = Gender.Male,
            Role = Role.User,
            RefreshToken = hashedRefreshToken,
            RefreshTokenExpiryTime = new DateTime(2030, 12, 31, 23, 59, 59, DateTimeKind.Utc)
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        _tokenService.HashRefreshToken("valid_refresh_token").Returns(hashedRefreshToken);
        _tokenService.GenerateAccessToken(user.Id, user.Email, user.Name, Role.User).Returns("new_access_token");
        _tokenService.GenerateRefreshToken().Returns("new_refresh_token");
        _tokenService.HashRefreshToken("new_refresh_token").Returns("new_hashed_refresh_token");

        var handler = new RefreshTokenCommandHandler(_context, _tokenService, _configuration, _dateTimeProvider);
        var command = new RefreshTokenCommand { RefreshToken = "valid_refresh_token" };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Role.Should().Be(Role.User);
    }

    /// <summary>
    /// Verifies that RefreshTokenCommandHandler includes Administrator role in new authentication result.
    /// </summary>
    [Fact]
    public async Task RefreshTokenCommandHandler_WhenUserHasAdministratorRole_IncludesRoleInResult()
    {
        // Arrange
        var hashedRefreshToken = "hashed_token";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@example.com",
            Name = "System Administrator",
            IsActive = true,
            Gender = Gender.Other,
            Role = Role.Administrator,
            RefreshToken = hashedRefreshToken,
            RefreshTokenExpiryTime = new DateTime(2030, 12, 31, 23, 59, 59, DateTimeKind.Utc)
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        _tokenService.HashRefreshToken("valid_refresh_token").Returns(hashedRefreshToken);
        _tokenService.GenerateAccessToken(user.Id, user.Email, user.Name, Role.Administrator).Returns("new_access_token");
        _tokenService.GenerateRefreshToken().Returns("new_refresh_token");
        _tokenService.HashRefreshToken("new_refresh_token").Returns("new_hashed_refresh_token");

        var handler = new RefreshTokenCommandHandler(_context, _tokenService, _configuration, _dateTimeProvider);
        var command = new RefreshTokenCommand { RefreshToken = "valid_refresh_token" };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Role.Should().Be(Role.Administrator);
    }

    /// <summary>
    /// Verifies that RefreshTokenCommandHandler passes user role to TokenService when generating new access token.
    /// </summary>
    [Fact]
    public async Task RefreshTokenCommandHandler_GeneratesAccessToken_WithUserRole()
    {
        // Arrange
        var hashedRefreshToken = "hashed_token";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            Name = "Test User",
            IsActive = true,
            Gender = Gender.Male,
            Role = Role.User,
            RefreshToken = hashedRefreshToken,
            RefreshTokenExpiryTime = new DateTime(2030, 12, 31, 23, 59, 59, DateTimeKind.Utc)
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        _tokenService.HashRefreshToken("valid_refresh_token").Returns(hashedRefreshToken);
        _tokenService.GenerateAccessToken(user.Id, user.Email, user.Name, Role.User).Returns("new_access_token");
        _tokenService.GenerateRefreshToken().Returns("new_refresh_token");
        _tokenService.HashRefreshToken("new_refresh_token").Returns("new_hashed_refresh_token");

        var handler = new RefreshTokenCommandHandler(_context, _tokenService, _configuration, _dateTimeProvider);
        var command = new RefreshTokenCommand { RefreshToken = "valid_refresh_token" };

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _tokenService.Received(1).GenerateAccessToken(
            user.Id,
            user.Email,
            user.Name,
            Role.User,
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that authentication result preserves role through login and refresh token flow.
    /// </summary>
    [Fact]
    public async Task AuthenticationFlow_RoleIsPreservedThroughLoginAndRefresh()
    {
        // Arrange - First: login with User role
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            Name = "Test User",
            PasswordHash = "hashed_password",
            IsActive = true,
            Gender = Gender.Male,
            Role = Role.User
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        _passwordHasher.VerifyPassword("password123", "hashed_password").Returns(true);
        _tokenService.GenerateAccessToken(user.Id, user.Email, user.Name, Role.User).Returns("access_token");
        _tokenService.GenerateRefreshToken().Returns("refresh_token");
        _tokenService.HashRefreshToken("refresh_token").Returns("hashed_refresh_token");

        var loginHandler = new LoginCommandHandler(_context, _passwordHasher, _tokenService, _configuration, _dateTimeProvider);
        var loginCommand = new LoginCommand { Email = "user@example.com", Password = "password123" };

        // Act - Login
        var loginResult = await loginHandler.Handle(loginCommand, CancellationToken.None);

        // Assert - Role is User from login
        loginResult.Role.Should().Be(Role.User);

        // Arrange - Second: refresh token to get new access token
        _tokenService.HashRefreshToken("refresh_token").Returns("hashed_refresh_token");
        _tokenService.GenerateAccessToken(user.Id, user.Email, user.Name, Role.User).Returns("new_access_token");
        _tokenService.GenerateRefreshToken().Returns("new_refresh_token");
        _tokenService.HashRefreshToken("new_refresh_token").Returns("new_hashed_refresh_token");

        var refreshHandler = new RefreshTokenCommandHandler(_context, _tokenService, _configuration, _dateTimeProvider);
        var refreshCommand = new RefreshTokenCommand { RefreshToken = "refresh_token" };

        // Act - Refresh
        var refreshResult = await refreshHandler.Handle(refreshCommand, CancellationToken.None);

        // Assert - Role is preserved as User after refresh
        refreshResult.Role.Should().Be(Role.User);
    }
}
