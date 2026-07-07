using FluentAssertions;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Application.Users.Commands.UpdateUserAsAdmin;
using SportsClubEventManager.Domain.Entities;
using SportsClubEventManager.Domain.Enums;

namespace SportsClubEventManager.Application.Users.Commands.UpdateUserAsAdmin;

/// <summary>
/// Unit tests for UpdateUserAsAdminCommandHandler verifying admin user editing with audit logging.
/// </summary>
public sealed class UpdateUserAsAdminCommandHandlerTests
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly UpdateUserAsAdminCommandHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateUserAsAdminCommandHandlerTests"/> class.
    /// </summary>
    public UpdateUserAsAdminCommandHandlerTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
        _auditService = Substitute.For<IAuditService>();
        _handler = new UpdateUserAsAdminCommandHandler(_context, _auditService);
    }

    /// <summary>
    /// Verifies that admin can update a user's name successfully.
    /// </summary>
    [Fact]
    public async Task Handle_ValidUpdate_UpdatesUserAndCreatesAuditLog()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Name = "Old Name",
            Email = "user@example.com",
            Role = Role.User,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        var command = new UpdateUserAsAdminCommand
        {
            AdminUserId = adminId,
            UserId = userId,
            Name = "New Name",
            Email = "user@example.com",
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0"
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        user.Name.Should().Be("New Name");
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _auditService.Received(1).LogAsync(
            AuditAction.UserUpdated,
            adminId,
            userId,
            "user@example.com",
            Arg.Any<string>(),
            "192.168.1.1",
            "Mozilla/5.0",
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that user not found throws KeyNotFoundException.
    /// </summary>
    [Fact]
    public async Task Handle_UserNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var users = new List<User>().BuildMockDbSet();
        _context.Users.Returns(users);

        var command = new UpdateUserAsAdminCommand
        {
            AdminUserId = adminId,
            UserId = userId,
            Name = "New Name",
            Email = "user@example.com"
        };

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    /// <summary>
    /// Verifies that email uniqueness is validated when changing email.
    /// </summary>
    [Fact]
    public async Task Handle_EmailAlreadyInUse_ThrowsInvalidOperationException()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Name = "User 1",
            Email = "user1@example.com",
            Role = Role.User,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var otherUser = new User
        {
            Id = otherUserId,
            Name = "User 2",
            Email = "user2@example.com",
            Role = Role.User,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var users = new List<User> { user, otherUser }.BuildMockDbSet();
        _context.Users.Returns(users);

        var command = new UpdateUserAsAdminCommand
        {
            AdminUserId = adminId,
            UserId = userId,
            Name = "User 1",
            Email = "user2@example.com" // Already in use
        };

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    /// <summary>
    /// Verifies that email can be changed to a new email without conflict.
    /// </summary>
    [Fact]
    public async Task Handle_ChangeEmailToUnused_UpdatesSuccessfully()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Name = "User Name",
            Email = "old@example.com",
            Role = Role.User,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        var command = new UpdateUserAsAdminCommand
        {
            AdminUserId = adminId,
            UserId = userId,
            Name = "User Name",
            Email = "new@example.com"
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        user.Email.Should().Be("new@example.com");
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that admin can update their own account (self-modification).
    /// </summary>
    [Fact]
    public async Task Handle_AdminUpdatingOwnAccount_UpdatesSuccessfully()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var admin = new User
        {
            Id = adminId,
            Name = "Admin User",
            Email = "admin@example.com",
            Role = Role.Administrator,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var users = new List<User> { admin }.BuildMockDbSet();
        _context.Users.Returns(users);

        var command = new UpdateUserAsAdminCommand
        {
            AdminUserId = adminId,
            UserId = adminId, // Self-modification
            Name = "Updated Admin",
            Email = "admin@example.com"
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        admin.Name.Should().Be("Updated Admin");
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that changes JSON is captured in audit log.
    /// </summary>
    [Fact]
    public async Task Handle_UpdateMultipleFields_CapturesChangesInAuditLog()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Name = "Old Name",
            Email = "old@example.com",
            Role = Role.User,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        var command = new UpdateUserAsAdminCommand
        {
            AdminUserId = adminId,
            UserId = userId,
            Name = "New Name",
            Email = "new@example.com"
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _auditService.Received(1).LogAsync(
            AuditAction.UserUpdated,
            adminId,
            userId,
            "new@example.com",
            Arg.Is<string>(s => s.Contains("Old Name") && s.Contains("New Name")),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that email unchanged allows update of other fields.
    /// </summary>
    [Fact]
    public async Task Handle_EmailUnchanged_UpdatesOtherFields()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Name = "Old Name",
            Email = "user@example.com",
            Role = Role.User,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        var command = new UpdateUserAsAdminCommand
        {
            AdminUserId = adminId,
            UserId = userId,
            Name = "New Name",
            Email = "user@example.com" // Same email
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        user.Name.Should().Be("New Name");
        user.Email.Should().Be("user@example.com");
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that IP address is captured in audit log.
    /// </summary>
    [Fact]
    public async Task Handle_WithIpAddress_CapturesIpInAuditLog()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Name = "User",
            Email = "user@example.com",
            Role = Role.User,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        var ipAddress = "10.20.30.40";
        var command = new UpdateUserAsAdminCommand
        {
            AdminUserId = adminId,
            UserId = userId,
            Name = "Updated",
            Email = "user@example.com",
            IpAddress = ipAddress
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _auditService.Received(1).LogAsync(
            Arg.Any<AuditAction>(),
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            ipAddress,
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that User-Agent is captured in audit log.
    /// </summary>
    [Fact]
    public async Task Handle_WithUserAgent_CapturesUserAgentInAuditLog()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Name = "User",
            Email = "user@example.com",
            Role = Role.User,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        var userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";
        var command = new UpdateUserAsAdminCommand
        {
            AdminUserId = adminId,
            UserId = userId,
            Name = "Updated",
            Email = "user@example.com",
            UserAgent = userAgent
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _auditService.Received(1).LogAsync(
            Arg.Any<AuditAction>(),
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            userAgent,
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that inactive user can be updated.
    /// </summary>
    [Fact]
    public async Task Handle_InactiveUser_UpdatesSuccessfully()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Name = "Inactive User",
            Email = "inactive@example.com",
            Role = Role.User,
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        var command = new UpdateUserAsAdminCommand
        {
            AdminUserId = adminId,
            UserId = userId,
            Name = "Updated Inactive",
            Email = "inactive@example.com"
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        user.Name.Should().Be("Updated Inactive");
        user.IsActive.Should().BeFalse(); // Status unchanged
    }
}
