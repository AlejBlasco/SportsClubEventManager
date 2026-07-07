using FluentAssertions;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Application.Users.Queries.GetUserById;
using SportsClubEventManager.Domain.Entities;
using SportsClubEventManager.Domain.Enums;

namespace SportsClubEventManager.Application.Users.Queries.GetUserById;

/// <summary>
/// Unit tests for GetUserByIdQueryHandler verifying user details retrieval.
/// </summary>
public sealed class GetUserByIdQueryHandlerTests
{
    private readonly IApplicationDbContext _context;
    private readonly GetUserByIdQueryHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetUserByIdQueryHandlerTests"/> class.
    /// </summary>
    public GetUserByIdQueryHandlerTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
        _handler = new GetUserByIdQueryHandler(_context);
    }

    /// <summary>
    /// Verifies that a valid user ID returns complete user details.
    /// </summary>
    [Fact]
    public async Task Handle_ValidUserId_ReturnsUserDetails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Name = "John Doe",
            Email = "john@example.com",
            Role = Role.Administrator,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            LastLoginAt = DateTime.UtcNow.AddHours(-2),
            ProviderName = null
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        var query = new GetUserByIdQuery { UserId = userId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(userId);
        result.Name.Should().Be("John Doe");
        result.Email.Should().Be("john@example.com");
        result.Role.Should().Be(Role.Administrator);
        result.IsActive.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that a non-existent user ID throws KeyNotFoundException.
    /// </summary>
    [Fact]
    public async Task Handle_UserNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var users = new List<User>().BuildMockDbSet();
        _context.Users.Returns(users);

        var query = new GetUserByIdQuery { UserId = userId };

        // Act
        Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    /// <summary>
    /// Verifies that inactive user details can still be retrieved.
    /// </summary>
    [Fact]
    public async Task Handle_InactiveUser_ReturnsUserDetailsWithInactiveStatus()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Name = "Inactive User",
            Email = "inactive@example.com",
            Role = Role.User,
            IsActive = false,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            LastLoginAt = DateTime.UtcNow.AddDays(-15),
            ProviderName = null
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        var query = new GetUserByIdQuery { UserId = userId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsActive.Should().BeFalse();
        result.Name.Should().Be("Inactive User");
    }

    /// <summary>
    /// Verifies that OAuth user data is correctly returned.
    /// </summary>
    [Fact]
    public async Task Handle_OAuthUser_ReturnsUserWithProviderName()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Name = "OAuth User",
            Email = "oauth@example.com",
            Role = Role.User,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            LastLoginAt = DateTime.UtcNow.AddHours(-1),
            ProviderName = "Google",
            ExternalProviderId = "google_12345"
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        var query = new GetUserByIdQuery { UserId = userId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ProviderName.Should().Be("Google");
    }

    /// <summary>
    /// Verifies that LastLoginAt field is correctly populated.
    /// </summary>
    [Fact]
    public async Task Handle_UserWithLastLogin_ReturnsLastLoginAt()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var lastLogin = DateTime.UtcNow.AddHours(-5);
        var user = new User
        {
            Id = userId,
            Name = "Active User",
            Email = "active@example.com",
            Role = Role.User,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            LastLoginAt = lastLogin,
            ProviderName = null
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        var query = new GetUserByIdQuery { UserId = userId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.LastLoginAt.Should().BeCloseTo(lastLogin, TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Verifies that user with null LastLoginAt returns null value.
    /// </summary>
    [Fact]
    public async Task Handle_UserNeverLoggedIn_LastLoginAtIsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Name = "New User",
            Email = "new@example.com",
            Role = Role.User,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = null,
            ProviderName = null
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        var query = new GetUserByIdQuery { UserId = userId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.LastLoginAt.Should().BeNull();
    }

    /// <summary>
    /// Verifies that all role types can be retrieved correctly.
    /// </summary>
    [Fact]
    public async Task Handle_AdminUser_ReturnsAdministratorRole()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Name = "Admin User",
            Email = "admin@example.com",
            Role = Role.Administrator,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            LastLoginAt = DateTime.UtcNow,
            ProviderName = null
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        var query = new GetUserByIdQuery { UserId = userId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Role.Should().Be(Role.Administrator);
    }

    /// <summary>
    /// Verifies that CreatedAt timestamp is preserved correctly.
    /// </summary>
    [Fact]
    public async Task Handle_UserCreatedAt_ReturnsCorrectCreationTimestamp()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var createdAt = new DateTime(2026, 01, 15, 10, 30, 00, DateTimeKind.Utc);
        var user = new User
        {
            Id = userId,
            Name = "Test User",
            Email = "test@example.com",
            Role = Role.User,
            IsActive = true,
            CreatedAt = createdAt,
            LastLoginAt = null,
            ProviderName = null
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        var query = new GetUserByIdQuery { UserId = userId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.CreatedAt.Should().Be(createdAt);
    }

    /// <summary>
    /// Verifies that multiple users in database only returns the specified user.
    /// </summary>
    [Fact]
    public async Task Handle_MultipleUsersInDatabase_ReturnsOnlyRequestedUser()
    {
        // Arrange
        var targetUserId = Guid.NewGuid();
        var users = new List<User>
        {
            new() { Id = Guid.NewGuid(), Name = "User1", Email = "user1@example.com", Role = Role.User, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = targetUserId, Name = "Target User", Email = "target@example.com", Role = Role.Administrator, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "User3", Email = "user3@example.com", Role = Role.User, IsActive = false, CreatedAt = DateTime.UtcNow }
        };

        var usersDbSet = users.BuildMockDbSet();
        _context.Users.Returns(usersDbSet);

        var query = new GetUserByIdQuery { UserId = targetUserId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Id.Should().Be(targetUserId);
        result.Name.Should().Be("Target User");
        result.Email.Should().Be("target@example.com");
    }
}
