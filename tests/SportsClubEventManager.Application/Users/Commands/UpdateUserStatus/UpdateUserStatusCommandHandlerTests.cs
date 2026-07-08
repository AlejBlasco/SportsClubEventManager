using FluentAssertions;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Application.Users.Commands.UpdateUserStatus;
using SportsClubEventManager.Domain.Entities;
using SportsClubEventManager.Domain.Enums;

namespace SportsClubEventManager.Application.Users.Commands.UpdateUserStatus;

/// <summary>
/// Unit tests for UpdateUserStatusCommandHandler verifying account activation/deactivation with audit logging.
/// </summary>
public sealed class UpdateUserStatusCommandHandlerTests
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly UpdateUserStatusCommandHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateUserStatusCommandHandlerTests"/> class.
    /// </summary>
    public UpdateUserStatusCommandHandlerTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
        _auditService = Substitute.For<IAuditService>();
        _handler = new UpdateUserStatusCommandHandler(_context, _auditService);
    }

    /// <summary>
    /// Verifies that an active user can be deactivated successfully.
    /// </summary>
    [Fact]
    public async Task Handle_DeactivateActiveUser_SucceedsAndLogsAudit()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Name = "Active User",
            Email = "user@example.com",
            Role = Role.User,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        var command = new UpdateUserStatusCommand
        {
            AdminUserId = adminId,
            UserId = userId,
            IsActive = false,
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0"
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        user.IsActive.Should().BeFalse();
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _auditService.Received(1).LogAsync(
            AuditAction.UserDeactivated,
            adminId,
            userId,
            "user@example.com",
            Arg.Any<string>(),
            "192.168.1.1",
            "Mozilla/5.0",
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that an inactive user can be activated successfully.
    /// </summary>
    [Fact]
    public async Task Handle_ActivateInactiveUser_SucceedsAndLogsAudit()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Name = "Inactive User",
            Email = "user@example.com",
            Role = Role.User,
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        var command = new UpdateUserStatusCommand
        {
            AdminUserId = adminId,
            UserId = userId,
            IsActive = true
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        user.IsActive.Should().BeTrue();
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _auditService.Received(1).LogAsync(
            AuditAction.UserActivated,
            adminId,
            userId,
            "user@example.com",
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
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

        var command = new UpdateUserStatusCommand
        {
            AdminUserId = adminId,
            UserId = userId,
            IsActive = false
        };

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"User with ID {userId} was not found.");
    }

    /// <summary>
    /// Verifies that deactivating already deactivated user succeeds (idempotent).
    /// </summary>
    [Fact]
    public async Task Handle_DeactivateAlreadyInactiveUser_SucceedsIdempotently()
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
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        var command = new UpdateUserStatusCommand
        {
            AdminUserId = adminId,
            UserId = userId,
            IsActive = false
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        user.IsActive.Should().BeFalse();
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that activating already active user succeeds (idempotent).
    /// </summary>
    [Fact]
    public async Task Handle_ActivateAlreadyActiveUser_SucceedsIdempotently()
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

        var command = new UpdateUserStatusCommand
        {
            AdminUserId = adminId,
            UserId = userId,
            IsActive = true
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        user.IsActive.Should().BeTrue();
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that admin can deactivate themselves.
    /// </summary>
    [Fact]
    public async Task Handle_AdminDeactivatingOwnAccount_Succeeds()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var admin = new User
        {
            Id = adminId,
            Name = "Admin",
            Email = "admin@example.com",
            Role = Role.Administrator,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var users = new List<User> { admin }.BuildMockDbSet();
        _context.Users.Returns(users);

        var command = new UpdateUserStatusCommand
        {
            AdminUserId = adminId,
            UserId = adminId, // Self-deactivation
            IsActive = false
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        admin.IsActive.Should().BeFalse();
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

        var ipAddress = "203.0.113.42";
        var command = new UpdateUserStatusCommand
        {
            AdminUserId = adminId,
            UserId = userId,
            IsActive = false,
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

        var userAgent = "Chrome/91.0.4472.124";
        var command = new UpdateUserStatusCommand
        {
            AdminUserId = adminId,
            UserId = userId,
            IsActive = false,
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
    /// Verifies that deactivating Administrator user succeeds (role demotion/deletion is blocked separately).
    /// </summary>
    [Fact]
    public async Task Handle_DeactivateAdministratorUser_Succeeds()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var targetAdminId = Guid.NewGuid();
        var admin = new User
        {
            Id = targetAdminId,
            Name = "Other Admin",
            Email = "admin@example.com",
            Role = Role.Administrator,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var users = new List<User> { admin }.BuildMockDbSet();
        _context.Users.Returns(users);

        var command = new UpdateUserStatusCommand
        {
            AdminUserId = adminId,
            UserId = targetAdminId,
            IsActive = false
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        admin.IsActive.Should().BeFalse();
        admin.Role.Should().Be(Role.Administrator); // Role unchanged
    }

    /// <summary>
    /// Verifies that deactivation creates audit log with correct action type.
    /// </summary>
    [Fact]
    public async Task Handle_Deactivate_LogsUserDeactivatedAction()
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

        var command = new UpdateUserStatusCommand
        {
            AdminUserId = adminId,
            UserId = userId,
            IsActive = false
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _auditService.Received(1).LogAsync(
            AuditAction.UserDeactivated,
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that activation creates audit log with correct action type.
    /// </summary>
    [Fact]
    public async Task Handle_Activate_LogsUserActivatedAction()
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
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        var command = new UpdateUserStatusCommand
        {
            AdminUserId = adminId,
            UserId = userId,
            IsActive = true
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _auditService.Received(1).LogAsync(
            AuditAction.UserActivated,
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
}
