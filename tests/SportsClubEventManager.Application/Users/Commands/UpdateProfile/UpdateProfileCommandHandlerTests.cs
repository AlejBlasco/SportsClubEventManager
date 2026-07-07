using FluentAssertions;
using Microsoft.Extensions.Logging;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Application.Users.Commands.UpdateProfile;
using SportsClubEventManager.Domain.Entities;
using SportsClubEventManager.Domain.Enums;

namespace SportsClubEventManager.Application.Users.Commands.UpdateProfile;

/// <summary>
/// Unit tests for UpdateProfileCommandHandler.
/// </summary>
public sealed class UpdateProfileCommandHandlerTests
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<UpdateProfileCommandHandler> _logger;
    private readonly UpdateProfileCommandHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateProfileCommandHandlerTests"/> class.
    /// </summary>
    public UpdateProfileCommandHandlerTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
        _logger = Substitute.For<ILogger<UpdateProfileCommandHandler>>();
        _handler = new UpdateProfileCommandHandler(_context, _logger);
    }

    /// <summary>
    /// Verifies that a valid local auth user can update their profile.
    /// </summary>
    [Fact]
    public async Task Handle_ValidLocalAuthUser_UpdatesProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Name = "Old Name",
            Gender = Gender.Male,
            Email = "old@example.com",
            LicenseNumber = "OLD123",
            LicenseCategory = "A",
            PasswordHash = "hashed",
            ProviderName = null,
            Role = Role.User,
            CreatedAt = DateTime.UtcNow
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        var command = new UpdateProfileCommand
        {
            RequestingUserId = userId,
            UserId = userId,
            Name = "New Name",
            Gender = "Female",
            Email = "new@example.com",
            LicenseNumber = "NEW456",
            LicenseCategory = "B"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("New Name");
        result.Gender.Should().Be("Female");
        result.Email.Should().Be("new@example.com");
        result.LicenseNumber.Should().Be("NEW456");
        result.LicenseCategory.Should().Be("B");

        user.Name.Should().Be("New Name");
        user.Gender.Should().Be(Gender.Female);
        user.Email.Should().Be("new@example.com");

        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that an OAuth user can update profile except email.
    /// </summary>
    [Fact]
    public async Task Handle_OAuthUserNoEmailChange_UpdatesProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Name = "OAuth User",
            Gender = Gender.Male,
            Email = "oauth@example.com",
            ProviderName = "Google",
            ExternalProviderId = "google_123",
            Role = Role.User,
            CreatedAt = DateTime.UtcNow
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        var command = new UpdateProfileCommand
        {
            RequestingUserId = userId,
            UserId = userId,
            Name = "Updated OAuth User",
            Gender = "Female",
            Email = "oauth@example.com", // Same email
            LicenseNumber = "LIC123",
            LicenseCategory = "C"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Name.Should().Be("Updated OAuth User");
        result.Gender.Should().Be("Female");
        result.Email.Should().Be("oauth@example.com");

        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
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

        var command = new UpdateProfileCommand
        {
            RequestingUserId = otherUserId,
            UserId = userId,
            Name = "Hacker",
            Gender = "Male",
            Email = "hacker@example.com"
        };

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("You can only update your own profile.");
    }


    /// <summary>
    /// Verifies that KeyNotFoundException is thrown when user not found.
    /// </summary>
    [Fact]
    public async Task Handle_UserNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var users = new List<User>().BuildMockDbSet();
        _context.Users.Returns(users);

        var command = new UpdateProfileCommand
        {
            RequestingUserId = userId,
            UserId = userId,
            Name = "Test",
            Gender = "Male",
            Email = "test@example.com"
        };

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"User with ID {userId} not found.");
    }

    /// <summary>
    /// Verifies that email uniqueness is validated.
    /// </summary>
    [Fact]
    public async Task Handle_EmailAlreadyExists_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Name = "User 1",
            Gender = Gender.Male,
            Email = "user1@example.com",
            PasswordHash = "hashed",
            Role = Role.User,
            CreatedAt = DateTime.UtcNow
        };

        var otherUser = new User
        {
            Id = otherUserId,
            Name = "User 2",
            Gender = Gender.Female,
            Email = "user2@example.com",
            PasswordHash = "hashed",
            Role = Role.User,
            CreatedAt = DateTime.UtcNow
        };

        var users = new List<User> { user, otherUser }.BuildMockDbSet();
        _context.Users.Returns(users);

        var command = new UpdateProfileCommand
        {
            RequestingUserId = userId,
            UserId = userId,
            Name = "User 1",
            Gender = "Male",
            Email = "user2@example.com" // Trying to use other user's email
        };

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("This email address is already registered.");
    }

    /// <summary>
    /// Verifies that OAuth users cannot change their email.
    /// </summary>
    [Fact]
    public async Task Handle_OAuthUserAttemptsEmailChange_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Name = "OAuth User",
            Gender = Gender.Male,
            Email = "oauth@example.com",
            ProviderName = "Google",
            Role = Role.User,
            CreatedAt = DateTime.UtcNow
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        var command = new UpdateProfileCommand
        {
            RequestingUserId = userId,
            UserId = userId,
            Name = "OAuth User",
            Gender = "Male",
            Email = "newemail@example.com" // Attempting email change
        };

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Email is managed by Google and cannot be changed here.");
    }

    /// <summary>
    /// Verifies that invalid gender value throws InvalidOperationException.
    /// </summary>
    [Fact]
    public async Task Handle_InvalidGenderValue_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Name = "Test User",
            Gender = Gender.Male,
            Email = "test@example.com",
            Role = Role.User,
            CreatedAt = DateTime.UtcNow
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        var command = new UpdateProfileCommand
        {
            RequestingUserId = userId,
            UserId = userId,
            Name = "Test User",
            Gender = "Invalid",
            Email = "test@example.com"
        };

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Invalid gender value: Invalid");
    }

    /// <summary>
    /// Verifies that email uniqueness check excludes own user ID.
    /// </summary>
    [Fact]
    public async Task Handle_EmailUnchanged_AllowsNoOpUpdate()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Name = "Test User",
            Gender = Gender.Male,
            Email = "test@example.com",
            Role = Role.User,
            CreatedAt = DateTime.UtcNow
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        var command = new UpdateProfileCommand
        {
            RequestingUserId = userId,
            UserId = userId,
            Name = "Updated Name",
            Gender = "Male",
            Email = "test@example.com" // Same email
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Name.Should().Be("Updated Name");
        result.Email.Should().Be("test@example.com");
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that optional fields can be null.
    /// </summary>
    [Fact]
    public async Task Handle_OptionalFieldsNull_Succeeds()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Name = "Test User",
            Gender = Gender.Male,
            Email = "test@example.com",
            Role = Role.User,
            CreatedAt = DateTime.UtcNow
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        var command = new UpdateProfileCommand
        {
            RequestingUserId = userId,
            UserId = userId,
            Name = "Test User",
            Gender = "Male",
            Email = "test@example.com",
            LicenseNumber = null,
            LicenseCategory = null
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.LicenseNumber.Should().BeNull();
        result.LicenseCategory.Should().BeNull();
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

}
