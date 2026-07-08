using FluentAssertions;
using Microsoft.Extensions.Configuration;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;
using SportsClubEventManager.Application.Authentication.Commands.RefreshToken;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Domain.Entities;
using SportsClubEventManager.Domain.Enums;

namespace SportsClubEventManager.Application.Authentication.Commands.RefreshToken;

/// <summary>
/// Unit tests for the RefreshTokenCommandHandler.
/// </summary>
public sealed class RefreshTokenCommandHandlerTests
{
    private readonly IApplicationDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly RefreshTokenCommandHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshTokenCommandHandlerTests"/> class.
    /// </summary>
    public RefreshTokenCommandHandlerTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
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

        _handler = new RefreshTokenCommandHandler(_context, _tokenService, _configuration, _dateTimeProvider);
    }

    /// <summary>
    /// Verifies that valid refresh token returns new authentication tokens.
    /// </summary>
    [Fact]
    public async Task Handle_WithValidRefreshToken_ReturnsNewAuthenticationResult()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Name = "Test User",
            RefreshToken = "hashed_refresh_token",
            RefreshTokenExpiryTime = new DateTime(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc).AddDays(7),
            IsActive = true,
            Gender = Gender.Male
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        _tokenService.HashRefreshToken("plain_refresh_token", Arg.Any<CancellationToken>()).Returns("hashed_refresh_token");
        _tokenService.GenerateAccessToken(user.Id, user.Email, user.Name, user.Role, Arg.Any<CancellationToken>()).Returns("new_access_token");
        _tokenService.GenerateRefreshToken(Arg.Any<CancellationToken>()).Returns("new_refresh_token");
        _tokenService.HashRefreshToken("new_refresh_token", Arg.Any<CancellationToken>()).Returns("new_hashed_refresh_token");

        var command = new RefreshTokenCommand { RefreshToken = "plain_refresh_token" };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(user.Id);
        result.Email.Should().Be(user.Email);
        result.Name.Should().Be(user.Name);
        result.AccessToken.Should().Be("new_access_token");
        result.RefreshToken.Should().Be("new_refresh_token");
        result.ExpiresIn.Should().Be(1800);

        user.RefreshToken.Should().Be("new_hashed_refresh_token");
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that invalid refresh token throws UnauthorizedAccessException.
    /// </summary>
    [Fact]
    public async Task Handle_WithInvalidRefreshToken_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var users = new List<User>().BuildMockDbSet();
        _context.Users.Returns(users);

        _tokenService.HashRefreshToken("invalid_token").Returns("hashed_invalid_token");

        var command = new RefreshTokenCommand { RefreshToken = "invalid_token" };

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid refresh token.");
    }

    /// <summary>
    /// Verifies that expired refresh token throws UnauthorizedAccessException.
    /// </summary>
    [Fact]
    public async Task Handle_WithExpiredRefreshToken_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Name = "Test User",
            RefreshToken = "hashed_refresh_token",
            RefreshTokenExpiryTime = new DateTime(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc).AddDays(-1),
            IsActive = true,
            Gender = Gender.Male
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        _tokenService.HashRefreshToken("plain_refresh_token").Returns("hashed_refresh_token");

        var command = new RefreshTokenCommand { RefreshToken = "plain_refresh_token" };

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Refresh token has expired. Please log in again.");
    }

    /// <summary>
    /// Verifies that inactive user with valid refresh token throws UnauthorizedAccessException.
    /// </summary>
    [Fact]
    public async Task Handle_WithInactiveUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Name = "Test User",
            RefreshToken = "hashed_refresh_token",
            RefreshTokenExpiryTime = new DateTime(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc).AddDays(7),
            IsActive = false,
            Gender = Gender.Male
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        _tokenService.HashRefreshToken("plain_refresh_token").Returns("hashed_refresh_token");

        var command = new RefreshTokenCommand { RefreshToken = "plain_refresh_token" };

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Account is inactive. Please contact support.");
    }

    /// <summary>
    /// Verifies that refresh token rotation invalidates old token.
    /// </summary>
    [Fact]
    public async Task Handle_WithValidRefreshToken_RotatesRefreshToken()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Name = "Test User",
            RefreshToken = "old_hashed_refresh_token",
            RefreshTokenExpiryTime = new DateTime(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc).AddDays(7),
            IsActive = true,
            Gender = Gender.Male
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        _tokenService.HashRefreshToken("old_refresh_token", Arg.Any<CancellationToken>()).Returns("old_hashed_refresh_token");
        _tokenService.GenerateAccessToken(user.Id, user.Email, user.Name, user.Role, Arg.Any<CancellationToken>()).Returns("new_access_token");
        _tokenService.GenerateRefreshToken(Arg.Any<CancellationToken>()).Returns("new_refresh_token");
        _tokenService.HashRefreshToken("new_refresh_token", Arg.Any<CancellationToken>()).Returns("new_hashed_refresh_token");

        var command = new RefreshTokenCommand { RefreshToken = "old_refresh_token" };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        user.RefreshToken.Should().Be("new_hashed_refresh_token");
        user.RefreshToken.Should().NotBe("old_hashed_refresh_token");
    }
}
