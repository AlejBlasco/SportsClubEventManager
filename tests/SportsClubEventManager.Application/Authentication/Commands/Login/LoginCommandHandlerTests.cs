using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;
using SportsClubEventManager.Application.Authentication.Commands.Login;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Domain.Entities;
using SportsClubEventManager.Domain.Enums;

namespace SportsClubEventManager.Application.Authentication.Commands.Login;

/// <summary>
/// Unit tests for the LoginCommandHandler.
/// </summary>
public sealed class LoginCommandHandlerTests
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly LoginCommandHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoginCommandHandlerTests"/> class.
    /// </summary>
    public LoginCommandHandlerTests()
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

        _handler = new LoginCommandHandler(_context, _passwordHasher, _tokenService, _configuration, _dateTimeProvider);
    }

    /// <summary>
    /// Verifies that valid credentials return a successful authentication result.
    /// </summary>
    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsAuthenticationResult()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Name = "Test User",
            PasswordHash = "hashed_password",
            IsActive = true,
            Gender = Gender.Male
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        _passwordHasher.VerifyPassword("password123", "hashed_password").Returns(true);
        _tokenService.GenerateAccessToken(user.Id, user.Email, user.Name).Returns("access_token");
        _tokenService.GenerateRefreshToken().Returns("refresh_token");
        _tokenService.HashRefreshToken("refresh_token").Returns("hashed_refresh_token");

        var command = new LoginCommand { Email = "test@example.com", Password = "password123" };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(user.Id);
        result.Email.Should().Be(user.Email);
        result.Name.Should().Be(user.Name);
        result.AccessToken.Should().Be("access_token");
        result.RefreshToken.Should().Be("refresh_token");
        result.ExpiresIn.Should().Be(1800);

        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that invalid email throws UnauthorizedAccessException.
    /// </summary>
    [Fact]
    public async Task Handle_WithInvalidEmail_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var users = new List<User>().BuildMockDbSet();
        _context.Users.Returns(users);

        var command = new LoginCommand { Email = "nonexistent@example.com", Password = "password123" };

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid email or password.");
    }

    /// <summary>
    /// Verifies that invalid password throws UnauthorizedAccessException.
    /// </summary>
    [Fact]
    public async Task Handle_WithInvalidPassword_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Name = "Test User",
            PasswordHash = "hashed_password",
            IsActive = true,
            Gender = Gender.Male
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        _passwordHasher.VerifyPassword("wrong_password", "hashed_password").Returns(false);

        var command = new LoginCommand { Email = "test@example.com", Password = "wrong_password" };

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid email or password.");
    }

    /// <summary>
    /// Verifies that inactive user account throws UnauthorizedAccessException.
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
            PasswordHash = "hashed_password",
            IsActive = false,
            Gender = Gender.Male
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        var command = new LoginCommand { Email = "test@example.com", Password = "password123" };

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Account is inactive. Please contact support.");
    }

    /// <summary>
    /// Verifies that external OAuth2 user attempting local login throws UnauthorizedAccessException.
    /// </summary>
    [Fact]
    public async Task Handle_WithExternalOAuth2User_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Name = "Test User",
            PasswordHash = null,
            ExternalProviderId = "google_id_123",
            ProviderName = "Google",
            IsActive = true,
            Gender = Gender.Male
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        var command = new LoginCommand { Email = "test@example.com", Password = "password123" };

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("This account uses external authentication. Please sign in with your OAuth2 provider.");
    }

    /// <summary>
    /// Verifies that successful login updates user's last login timestamp.
    /// </summary>
    [Fact]
    public async Task Handle_WithValidCredentials_UpdatesLastLoginAt()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Name = "Test User",
            PasswordHash = "hashed_password",
            IsActive = true,
            Gender = Gender.Male
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        _passwordHasher.VerifyPassword("password123", "hashed_password").Returns(true);
        _tokenService.GenerateAccessToken(user.Id, user.Email, user.Name).Returns("access_token");
        _tokenService.GenerateRefreshToken().Returns("refresh_token");
        _tokenService.HashRefreshToken("refresh_token").Returns("hashed_refresh_token");

        var command = new LoginCommand { Email = "test@example.com", Password = "password123" };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        user.LastLoginAt.Should().Be(new DateTime(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc));
        user.RefreshToken.Should().Be("hashed_refresh_token");
        user.RefreshTokenExpiryTime.Should().Be(new DateTime(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc).AddDays(7));
    }
}
