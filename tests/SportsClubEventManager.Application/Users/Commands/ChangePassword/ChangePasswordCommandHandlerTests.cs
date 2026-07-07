using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;
using SportsClubEventManager.Application.Authentication.Common;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Application.Users.Commands.ChangePassword;
using SportsClubEventManager.Domain.Entities;
using SportsClubEventManager.Domain.Enums;

namespace SportsClubEventManager.Application.Users.Commands.ChangePassword;

/// <summary>
/// Unit tests for ChangePasswordCommandHandler.
/// </summary>
public sealed class ChangePasswordCommandHandlerTests
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<ChangePasswordCommandHandler> _logger;
    private readonly ChangePasswordCommandHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChangePasswordCommandHandlerTests"/> class.
    /// </summary>
    public ChangePasswordCommandHandlerTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
        _passwordHasher = Substitute.For<IPasswordHasher>();
        _tokenService = Substitute.For<ITokenService>();
        _logger = Substitute.For<ILogger<ChangePasswordCommandHandler>>();

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

        _handler = new ChangePasswordCommandHandler(
            _context,
            _passwordHasher,
            _tokenService,
            _configuration,
            _dateTimeProvider,
            _logger);
    }

    /// <summary>
    /// Verifies that a valid password change updates password and returns new tokens.
    /// </summary>
    [Fact]
    public async Task Handle_ValidPasswordChange_UpdatesPasswordAndReturnsTokens()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Name = "Test User",
            Email = "test@example.com",
            PasswordHash = "old_hashed_password",
            ProviderName = null,
            Role = Role.User,
            Gender = Gender.Male,
            CreatedAt = DateTime.UtcNow
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        _passwordHasher.VerifyPassword("current_password", "old_hashed_password").Returns(true);
        _passwordHasher.HashPassword("new_password").Returns("new_hashed_password");
        _tokenService.GenerateAccessToken(userId, user.Email, user.Name, user.Role, Arg.Any<CancellationToken>()).Returns("new_access_token");
        _tokenService.GenerateRefreshToken(Arg.Any<CancellationToken>()).Returns("new_refresh_token");
        _tokenService.HashRefreshToken("new_refresh_token", Arg.Any<CancellationToken>()).Returns("hashed_refresh_token");

        var command = new ChangePasswordCommand
        {
            RequestingUserId = userId,
            UserId = userId,
            CurrentPassword = "current_password",
            NewPassword = "new_password"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("new_access_token");
        result.RefreshToken.Should().Be("new_refresh_token");
        result.UserId.Should().Be(userId);
        result.Email.Should().Be("test@example.com");
        result.ExpiresIn.Should().Be(1800);

        user.PasswordHash.Should().Be("new_hashed_password");
        user.RefreshToken.Should().Be("hashed_refresh_token");

        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that password is hashed with BCrypt.
    /// </summary>
    [Fact]
    public async Task Handle_PasswordChange_HashesPasswordWithBCrypt()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Name = "Test User",
            Email = "test@example.com",
            PasswordHash = "old_hash",
            Role = Role.User,
            Gender = Gender.Male,
            CreatedAt = DateTime.UtcNow
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        _passwordHasher.VerifyPassword("current", "old_hash").Returns(true);
        _passwordHasher.HashPassword("new_password").Returns("new_hash");
        _tokenService.GenerateAccessToken(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Role>(), Arg.Any<CancellationToken>()).Returns("token");
        _tokenService.GenerateRefreshToken(Arg.Any<CancellationToken>()).Returns("refresh");
        _tokenService.HashRefreshToken(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("hashed");

        var command = new ChangePasswordCommand
        {
            RequestingUserId = userId,
            UserId = userId,
            CurrentPassword = "current",
            NewPassword = "new_password"
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _passwordHasher.Received(1).HashPassword("new_password");
        user.PasswordHash.Should().Be("new_hash");
    }

    /// <summary>
    /// Verifies that new tokens are generated.
    /// </summary>
    [Fact]
    public async Task Handle_PasswordChange_GeneratesNewTokens()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Name = "Test User",
            Email = "test@example.com",
            PasswordHash = "hash",
            Role = Role.User,
            Gender = Gender.Male,
            CreatedAt = DateTime.UtcNow
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        _passwordHasher.VerifyPassword(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _passwordHasher.HashPassword(Arg.Any<string>()).Returns("new_hash");
        _tokenService.GenerateAccessToken(userId, user.Email, user.Name, user.Role, Arg.Any<CancellationToken>()).Returns("access");
        _tokenService.GenerateRefreshToken(Arg.Any<CancellationToken>()).Returns("refresh");
        _tokenService.HashRefreshToken("refresh", Arg.Any<CancellationToken>()).Returns("hashed_refresh");

        var command = new ChangePasswordCommand
        {
            RequestingUserId = userId,
            UserId = userId,
            CurrentPassword = "current",
            NewPassword = "new"
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _tokenService.Received(1).GenerateAccessToken(userId, user.Email, user.Name, user.Role, Arg.Any<CancellationToken>());
        _tokenService.Received(1).GenerateRefreshToken(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that refresh token is hashed before storage.
    /// </summary>
    [Fact]
    public async Task Handle_PasswordChange_HashesRefreshToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Name = "Test User",
            Email = "test@example.com",
            PasswordHash = "hash",
            Role = Role.User,
            Gender = Gender.Male,
            CreatedAt = DateTime.UtcNow
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        _passwordHasher.VerifyPassword(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _passwordHasher.HashPassword(Arg.Any<string>()).Returns("new_hash");
        _tokenService.GenerateAccessToken(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Role>(), Arg.Any<CancellationToken>()).Returns("access");
        _tokenService.GenerateRefreshToken(Arg.Any<CancellationToken>()).Returns("plain_refresh");
        _tokenService.HashRefreshToken("plain_refresh", Arg.Any<CancellationToken>()).Returns("hashed_refresh");

        var command = new ChangePasswordCommand
        {
            RequestingUserId = userId,
            UserId = userId,
            CurrentPassword = "current",
            NewPassword = "new"
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _tokenService.Received(1).HashRefreshToken("plain_refresh", Arg.Any<CancellationToken>());
        user.RefreshToken.Should().Be("hashed_refresh");
    }

    /// <summary>
    /// Verifies that refresh token expiry is set correctly.
    /// </summary>
    [Fact]
    public async Task Handle_PasswordChange_SetsRefreshTokenExpiry()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Name = "Test User",
            Email = "test@example.com",
            PasswordHash = "hash",
            Role = Role.User,
            Gender = Gender.Male,
            CreatedAt = DateTime.UtcNow
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        _passwordHasher.VerifyPassword(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _passwordHasher.HashPassword(Arg.Any<string>()).Returns("new_hash");
        _tokenService.GenerateAccessToken(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Role>(), Arg.Any<CancellationToken>()).Returns("access");
        _tokenService.GenerateRefreshToken(Arg.Any<CancellationToken>()).Returns("refresh");
        _tokenService.HashRefreshToken(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("hashed");

        var command = new ChangePasswordCommand
        {
            RequestingUserId = userId,
            UserId = userId,
            CurrentPassword = "current",
            NewPassword = "new"
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        var expectedExpiry = new DateTime(2030, 1, 8, 12, 0, 0, DateTimeKind.Utc);
        user.RefreshTokenExpiryTime.Should().Be(expectedExpiry);
    }

    /// <summary>
    /// Verifies that RequestingUserId must match UserId.
    /// </summary>
    [Fact]
    public async Task Handle_RequestingUserIdMismatch_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var command = new ChangePasswordCommand
        {
            RequestingUserId = otherUserId,
            UserId = userId,
            CurrentPassword = "current",
            NewPassword = "new"
        };

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("You can only change your own password.");
    }

    /// <summary>
    /// Verifies that user not found throws KeyNotFoundException.
    /// </summary>
    [Fact]
    public async Task Handle_UserNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var users = new List<User>().BuildMockDbSet();
        _context.Users.Returns(users);

        var command = new ChangePasswordCommand
        {
            RequestingUserId = userId,
            UserId = userId,
            CurrentPassword = "current",
            NewPassword = "new"
        };

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"User with ID {userId} not found.");
    }

    /// <summary>
    /// Verifies that OAuth users cannot change password.
    /// </summary>
    [Fact]
    public async Task Handle_OAuthUser_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Name = "OAuth User",
            Email = "oauth@example.com",
            ProviderName = "Google",
            Role = Role.User,
            Gender = Gender.Male,
            CreatedAt = DateTime.UtcNow
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        var command = new ChangePasswordCommand
        {
            RequestingUserId = userId,
            UserId = userId,
            CurrentPassword = "current",
            NewPassword = "new"
        };

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Password is managed by Google and cannot be changed here.");
    }

    /// <summary>
    /// Verifies that incorrect current password throws UnauthorizedAccessException.
    /// </summary>
    [Fact]
    public async Task Handle_IncorrectCurrentPassword_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Name = "Test User",
            Email = "test@example.com",
            PasswordHash = "hashed_password",
            Role = Role.User,
            Gender = Gender.Male,
            CreatedAt = DateTime.UtcNow
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        _passwordHasher.VerifyPassword("wrong_password", "hashed_password").Returns(false);

        var command = new ChangePasswordCommand
        {
            RequestingUserId = userId,
            UserId = userId,
            CurrentPassword = "wrong_password",
            NewPassword = "new_password"
        };

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Current password is incorrect.");
    }



    /// <summary>
    /// Verifies that the result contains correct authentication data.
    /// </summary>
    [Fact]
    public async Task Handle_ReturnsAuthenticationResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Name = "Test User",
            Email = "test@example.com",
            PasswordHash = "hash",
            Role = Role.Administrator,
            Gender = Gender.Male,
            CreatedAt = DateTime.UtcNow
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        _passwordHasher.VerifyPassword(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _passwordHasher.HashPassword(Arg.Any<string>()).Returns("new_hash");
        _tokenService.GenerateAccessToken(userId, user.Email, user.Name, user.Role, Arg.Any<CancellationToken>()).Returns("access_token");
        _tokenService.GenerateRefreshToken(Arg.Any<CancellationToken>()).Returns("refresh_token");
        _tokenService.HashRefreshToken(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("hashed");

        var command = new ChangePasswordCommand
        {
            RequestingUserId = userId,
            UserId = userId,
            CurrentPassword = "current",
            NewPassword = "new"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.UserId.Should().Be(userId);
        result.Email.Should().Be("test@example.com");
        result.Name.Should().Be("Test User");
        result.Role.Should().Be(Role.Administrator);
        result.AccessToken.Should().Be("access_token");
        result.RefreshToken.Should().Be("refresh_token");
        result.ExpiresIn.Should().Be(1800);
    }
}
