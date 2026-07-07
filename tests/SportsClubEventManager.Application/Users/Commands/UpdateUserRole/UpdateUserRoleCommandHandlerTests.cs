using FluentAssertions;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Application.Users.Commands.UpdateUserRole;
using SportsClubEventManager.Domain.Entities;
using SportsClubEventManager.Domain.Enums;

namespace SportsClubEventManager.Application.Users.Commands.UpdateUserRole;

/// <summary>
/// Unit tests for UpdateUserRoleCommandHandler verifying role assignment with last-admin validation.
/// </summary>
public sealed class UpdateUserRoleCommandHandlerTests
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly UpdateUserRoleCommandHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateUserRoleCommandHandlerTests"/> class.
    /// </summary>
    public UpdateUserRoleCommandHandlerTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
        _auditService = Substitute.For<IAuditService>();
        _handler = new UpdateUserRoleCommandHandler(_context, _auditService);
    }

    /// <summary>
    /// Verifies that a user role can be changed successfully.
    /// </summary>
    [Fact]
    public async Task Handle_ChangeUserRole_SucceedsAndLogsAudit()
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

        var admin = new User
        {
            Id = adminId,
            Name = "Admin",
            Email = "admin@example.com",
            Role = Role.Administrator,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var users = new List<User> { user, admin }.BuildMockDbSet();
        _context.Users.Returns(users);

        var command = new UpdateUserRoleCommand
        {
            AdminUserId = adminId,
            UserId = userId,
            NewRole = Role.Administrator,
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0"
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        user.Role.Should().Be(Role.Administrator);
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _auditService.Received(1).LogAsync(
            AuditAction.RoleAssigned,
            adminId,
            userId,
            "user@example.com",
            Arg.Any<string>(),
            "192.168.1.1",
            "Mozilla/5.0",
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// CRITICAL: Verifies that demoting the last Administrator throws InvalidOperationException.
    /// </summary>
    [Fact]
    public async Task Handle_DemoteLastAdministrator_ThrowsInvalidOperationException()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var admin = new User
        {
            Id = adminId,
            Name = "Only Admin",
            Email = "admin@example.com",
            Role = Role.Administrator,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var users = new List<User> { admin }.BuildMockDbSet();
        _context.Users.Returns(users);

        var command = new UpdateUserRoleCommand
        {
            AdminUserId = adminId,
            UserId = adminId,
            NewRole = Role.User
        };

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot remove the Administrator role from the last administrator in the system.");
    }

    /// <summary>
    /// Verifies that demoting an Administrator succeeds when multiple admins exist.
    /// </summary>
    [Fact]
    public async Task Handle_DemoteAdministratorWhenMultipleExist_SucceedsWithRoleRemoved()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var targetAdminId = Guid.NewGuid();

        var admin = new User
        {
            Id = adminId,
            Name = "Admin 1",
            Email = "admin1@example.com",
            Role = Role.Administrator,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var targetAdmin = new User
        {
            Id = targetAdminId,
            Name = "Admin 2",
            Email = "admin2@example.com",
            Role = Role.Administrator,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var users = new List<User> { admin, targetAdmin }.BuildMockDbSet();
        _context.Users.Returns(users);

        var command = new UpdateUserRoleCommand
        {
            AdminUserId = adminId,
            UserId = targetAdminId,
            NewRole = Role.User
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        targetAdmin.Role.Should().Be(Role.User);
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that promoting a user to Administrator succeeds when other admins exist.
    /// </summary>
    [Fact]
    public async Task Handle_PromoteUserToAdministrator_SucceedsWithMultipleAdmins()
    {
        // Arrange
        var admin1Id = Guid.NewGuid();
        var admin2Id = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var admin1 = new User
        {
            Id = admin1Id,
            Name = "Admin 1",
            Email = "admin1@example.com",
            Role = Role.Administrator,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var admin2 = new User
        {
            Id = admin2Id,
            Name = "Admin 2",
            Email = "admin2@example.com",
            Role = Role.Administrator,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var user = new User
        {
            Id = userId,
            Name = "Regular User",
            Email = "user@example.com",
            Role = Role.User,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var users = new List<User> { admin1, admin2, user }.BuildMockDbSet();
        _context.Users.Returns(users);

        var command = new UpdateUserRoleCommand
        {
            AdminUserId = admin1Id,
            UserId = userId,
            NewRole = Role.Administrator
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        user.Role.Should().Be(Role.Administrator);
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
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

        var command = new UpdateUserRoleCommand
        {
            AdminUserId = adminId,
            UserId = userId,
            NewRole = Role.User
        };

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"User with ID {userId} was not found.");
    }

    /// <summary>
    /// Verifies that admin can change their own role (allowed, with warning expected in UI).
    /// </summary>
    [Fact]
    public async Task Handle_AdminChangingOwnRole_Succeeds()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var otherAdminId = Guid.NewGuid();

        var admin = new User
        {
            Id = adminId,
            Name = "Admin 1",
            Email = "admin1@example.com",
            Role = Role.Administrator,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var otherAdmin = new User
        {
            Id = otherAdminId,
            Name = "Admin 2",
            Email = "admin2@example.com",
            Role = Role.Administrator,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var users = new List<User> { admin, otherAdmin }.BuildMockDbSet();
        _context.Users.Returns(users);

        var command = new UpdateUserRoleCommand
        {
            AdminUserId = adminId,
            UserId = adminId,
            NewRole = Role.User
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        admin.Role.Should().Be(Role.User);
    }

    /// <summary>
    /// Verifies that same role assignment succeeds (idempotent).
    /// </summary>
    [Fact]
    public async Task Handle_SameRoleAssignment_SucceedsIdempotently()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Name = "User",
            Email = "user@example.com",
            Role = Role.Administrator,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _context.Users.Returns(users);

        var command = new UpdateUserRoleCommand
        {
            AdminUserId = adminId,
            UserId = userId,
            NewRole = Role.Administrator
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        user.Role.Should().Be(Role.Administrator);
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that changes are captured in audit log.
    /// </summary>
    [Fact]
    public async Task Handle_RoleChange_CapturesChangesInAuditLog()
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

        var admin = new User
        {
            Id = adminId,
            Name = "Admin",
            Email = "admin@example.com",
            Role = Role.Administrator,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var users = new List<User> { user, admin }.BuildMockDbSet();
        _context.Users.Returns(users);

        var command = new UpdateUserRoleCommand
        {
            AdminUserId = adminId,
            UserId = userId,
            NewRole = Role.Administrator
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _auditService.Received(1).LogAsync(
            AuditAction.RoleAssigned,
            adminId,
            userId,
            "user@example.com",
            Arg.Is<string>(s => s.Contains("User") && s.Contains("Administrator")),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
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

        var admin = new User
        {
            Id = adminId,
            Name = "Admin",
            Email = "admin@example.com",
            Role = Role.Administrator,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var users = new List<User> { user, admin }.BuildMockDbSet();
        _context.Users.Returns(users);

        var ipAddress = "10.0.0.1";
        var command = new UpdateUserRoleCommand
        {
            AdminUserId = adminId,
            UserId = userId,
            NewRole = Role.Administrator,
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

        var admin = new User
        {
            Id = adminId,
            Name = "Admin",
            Email = "admin@example.com",
            Role = Role.Administrator,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var users = new List<User> { user, admin }.BuildMockDbSet();
        _context.Users.Returns(users);

        var userAgent = "Safari/537.36";
        var command = new UpdateUserRoleCommand
        {
            AdminUserId = adminId,
            UserId = userId,
            NewRole = Role.Administrator,
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
    /// Verifies that demoting a user creates RoleRemoved audit action.
    /// </summary>
    [Fact]
    public async Task Handle_DemoteAdministrator_LogsRoleRemovedAction()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var targetAdminId = Guid.NewGuid();

        var admin = new User
        {
            Id = adminId,
            Name = "Admin 1",
            Email = "admin1@example.com",
            Role = Role.Administrator,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var targetAdmin = new User
        {
            Id = targetAdminId,
            Name = "Admin 2",
            Email = "admin2@example.com",
            Role = Role.Administrator,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var users = new List<User> { admin, targetAdmin }.BuildMockDbSet();
        _context.Users.Returns(users);

        var command = new UpdateUserRoleCommand
        {
            AdminUserId = adminId,
            UserId = targetAdminId,
            NewRole = Role.User
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _auditService.Received(1).LogAsync(
            AuditAction.RoleRemoved,
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that promoting a user creates RoleAssigned audit action.
    /// </summary>
    [Fact]
    public async Task Handle_PromoteUser_LogsRoleAssignedAction()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var admin = new User
        {
            Id = adminId,
            Name = "Admin",
            Email = "admin@example.com",
            Role = Role.Administrator,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var user = new User
        {
            Id = userId,
            Name = "User",
            Email = "user@example.com",
            Role = Role.User,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var users = new List<User> { admin, user }.BuildMockDbSet();
        _context.Users.Returns(users);

        var command = new UpdateUserRoleCommand
        {
            AdminUserId = adminId,
            UserId = userId,
            NewRole = Role.Administrator
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _auditService.Received(1).LogAsync(
            AuditAction.RoleAssigned,
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
}
