using FluentAssertions;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Application.Users.Queries.GetUserProfile;
using SportsClubEventManager.Domain.Entities;
using SportsClubEventManager.Domain.Enums;

namespace SportsClubEventManager.Application.Users.Queries.GetUserProfile;

/// <summary>
/// Unit tests for GetUserProfileQueryHandler.
/// </summary>
public sealed class GetUserProfileQueryHandlerTests
{
    private readonly IApplicationDbContext _context;
    private readonly GetUserProfileQueryHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetUserProfileQueryHandlerTests"/> class.
    /// </summary>
    public GetUserProfileQueryHandlerTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
        _handler = new GetUserProfileQueryHandler(_context);
    }

    /// <summary>
    /// Verifies that an existing user profile is returned successfully.
    /// </summary>
    [Fact]
    public async Task Handle_UserExists_ReturnsUserProfileDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Name = "John Doe",
            Gender = Gender.Male,
            Email = "john.doe@example.com",
            LicenseNumber = "B-12345678",
            LicenseCategory = "B",
            Role = Role.User,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            ProviderName = null
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        var query = new GetUserProfileQuery { UserId = userId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.Name.Should().Be("John Doe");
        result.Gender.Should().Be("Male");
        result.Email.Should().Be("john.doe@example.com");
        result.LicenseNumber.Should().Be("B-12345678");
        result.LicenseCategory.Should().Be("B");
        result.Role.Should().Be("User");
        result.CreatedAt.Should().Be(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        result.UpdatedAt.Should().Be(new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        result.IsOAuthUser.Should().BeFalse();
        result.ProviderName.Should().BeNull();
    }

    /// <summary>
    /// Verifies that a KeyNotFoundException is thrown when the user is not found.
    /// </summary>
    [Fact]
    public async Task Handle_UserNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var users = new List<User>().BuildMockDbSet();
        _context.Users.Returns(users);

        var query = new GetUserProfileQuery { UserId = userId };

        // Act
        Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"User with ID {userId} not found.");
    }

    /// <summary>
    /// Verifies that IsOAuthUser is false for local auth users.
    /// </summary>
    [Fact]
    public async Task Handle_LocalAuthUser_IsOAuthUserFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Name = "Jane Smith",
            Gender = Gender.Female,
            Email = "jane@example.com",
            PasswordHash = "hashed_password",
            ProviderName = null,
            Role = Role.User,
            CreatedAt = DateTime.UtcNow
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        var query = new GetUserProfileQuery { UserId = userId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsOAuthUser.Should().BeFalse();
        result.ProviderName.Should().BeNull();
    }

    /// <summary>
    /// Verifies that IsOAuthUser is true for OAuth users.
    /// </summary>
    [Fact]
    public async Task Handle_OAuthUser_IsOAuthUserTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Name = "OAuth User",
            Gender = Gender.Other,
            Email = "oauth@example.com",
            ProviderName = "Google",
            ExternalProviderId = "google_12345",
            Role = Role.User,
            CreatedAt = DateTime.UtcNow
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        var query = new GetUserProfileQuery { UserId = userId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsOAuthUser.Should().BeTrue();
        result.ProviderName.Should().Be("Google");
    }

    /// <summary>
    /// Verifies that UpdatedAt falls back to CreatedAt when null.
    /// </summary>
    [Fact]
    public async Task Handle_UpdatedAtNull_FallsBackToCreatedAt()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var createdDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var user = new User
        {
            Id = userId,
            Name = "New User",
            Gender = Gender.Male,
            Email = "new@example.com",
            Role = Role.User,
            CreatedAt = createdDate,
            UpdatedAt = null
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        var query = new GetUserProfileQuery { UserId = userId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.UpdatedAt.Should().Be(createdDate);
    }

    /// <summary>
    /// Verifies that Gender enum converts to string correctly.
    /// </summary>
    [Theory]
    [InlineData(Gender.Male, "Male")]
    [InlineData(Gender.Female, "Female")]
    [InlineData(Gender.Other, "Other")]
    public async Task Handle_GenderEnum_ConvertsToString(Gender gender, string expected)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Name = "Test User",
            Gender = gender,
            Email = "test@example.com",
            Role = Role.User,
            CreatedAt = DateTime.UtcNow
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        var query = new GetUserProfileQuery { UserId = userId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Gender.Should().Be(expected);
    }
}
