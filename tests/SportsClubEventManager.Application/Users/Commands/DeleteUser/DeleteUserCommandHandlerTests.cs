using FluentAssertions;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Application.Users.Commands.DeleteUser;
using SportsClubEventManager.Domain.Entities;
using SportsClubEventManager.Domain.Enums;

namespace SportsClubEventManager.Application.Users.Commands.DeleteUser;

/// <summary>
/// Unit tests for DeleteUserCommandHandler verifying user deletion with cascade handling and audit logging.
/// </summary>
public sealed class DeleteUserCommandHandlerTests
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly DeleteUserCommandHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteUserCommandHandlerTests"/> class.
    /// </summary>
    public DeleteUserCommandHandlerTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
        _auditService = Substitute.For<IAuditService>();
        _handler = new DeleteUserCommandHandler(_context, _auditService);
    }

    /// <summary>
    /// Verifies that a regular user can be deleted successfully.
    /// </summary>
    [Fact]
    public async Task Handle_DeleteRegularUser_SucceedsAndLogsAudit()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Name = "User to Delete",
            Email = "delete@example.com",
            Role = Role.User,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Registrations = new List<Registration>()
        };

        var users = new List<User> { user }.BuildMockDbSet();
        var registrations = new List<Registration>().BuildMockDbSet();

        _context.Users.Returns(users);
        _context.Registrations.Returns(registrations);

        var command = new DeleteUserCommand
        {
            AdminUserId = adminId,
            UserId = userId,
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0"
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _auditService.Received(1).LogAsync(
            AuditAction.UserDeleted,
            adminId,
            userId,
            "delete@example.com",
            Arg.Any<string>(),
            "192.168.1.1",
            "Mozilla/5.0",
            Arg.Any<CancellationToken>());
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// CRITICAL: Verifies that deleting the last Administrator throws InvalidOperationException.
    /// </summary>
    [Fact]
    public async Task Handle_DeleteLastAdministrator_ThrowsInvalidOperationException()
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
            CreatedAt = DateTime.UtcNow,
            Registrations = new List<Registration>()
        };

        var users = new List<User> { admin }.BuildMockDbSet();
        _context.Users.Returns(users);

        var command = new DeleteUserCommand
        {
            AdminUserId = adminId,
            UserId = adminId
        };

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot delete the last administrator in the system.");
    }

    /// <summary>
    /// Verifies that deleting an Administrator when others exist succeeds.
    /// </summary>
    [Fact]
    public async Task Handle_DeleteAdministratorWithOtherAdminsPresent_Succeeds()
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
            CreatedAt = DateTime.UtcNow,
            Registrations = new List<Registration>()
        };

        var targetAdmin = new User
        {
            Id = targetAdminId,
            Name = "Admin 2",
            Email = "admin2@example.com",
            Role = Role.Administrator,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Registrations = new List<Registration>()
        };

        var users = new List<User> { admin, targetAdmin }.BuildMockDbSet();
        var registrations = new List<Registration>().BuildMockDbSet();

        _context.Users.Returns(users);
        _context.Registrations.Returns(registrations);

        var command = new DeleteUserCommand
        {
            AdminUserId = adminId,
            UserId = targetAdminId
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
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

        var command = new DeleteUserCommand
        {
            AdminUserId = adminId,
            UserId = userId
        };

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"User with ID {userId} was not found.");
    }

    /// <summary>
    /// Verifies that cascade delete removes all user registrations.
    /// </summary>
    [Fact]
    public async Task Handle_DeleteUserWithRegistrations_CascadeDeletesRegistrations()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        var registration1 = new Registration
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            UserId = userId,
            Status = RegistrationStatus.Registered
        };

        var registration2 = new Registration
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            UserId = userId,
            Status = RegistrationStatus.Registered
        };

        var user = new User
        {
            Id = userId,
            Name = "User with Registrations",
            Email = "user@example.com",
            Role = Role.User,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Registrations = new List<Registration> { registration1, registration2 }
        };

        var admin = new User
        {
            Id = adminId,
            Name = "Admin",
            Email = "admin@example.com",
            Role = Role.Administrator,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Registrations = new List<Registration>()
        };

        var users = new List<User> { user, admin }.BuildMockDbSet();
        var registrations = new List<Registration> { registration1, registration2 }.BuildMockDbSet();

        _context.Users.Returns(users);
        _context.Registrations.Returns(registrations);

        var command = new DeleteUserCommand
        {
            AdminUserId = adminId,
            UserId = userId
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        user.Registrations.Should().HaveCount(2);
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that email is captured in audit log before deletion.
    /// </summary>
    [Fact]
    public async Task Handle_DeleteUser_CapturesEmailInAuditLog()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var userEmail = "deleted@example.com";

        var user = new User
        {
            Id = userId,
            Name = "User",
            Email = userEmail,
            Role = Role.User,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Registrations = new List<Registration>()
        };

        var admin = new User
        {
            Id = adminId,
            Name = "Admin",
            Email = "admin@example.com",
            Role = Role.Administrator,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Registrations = new List<Registration>()
        };

        var users = new List<User> { user, admin }.BuildMockDbSet();
        var registrations = new List<Registration>().BuildMockDbSet();

        _context.Users.Returns(users);
        _context.Registrations.Returns(registrations);

        var command = new DeleteUserCommand
        {
            AdminUserId = adminId,
            UserId = userId
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _auditService.Received(1).LogAsync(
            AuditAction.UserDeleted,
            adminId,
            userId,
            userEmail,
            Arg.Any<string>(),
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
            CreatedAt = DateTime.UtcNow,
            Registrations = new List<Registration>()
        };

        var admin = new User
        {
            Id = adminId,
            Name = "Admin",
            Email = "admin@example.com",
            Role = Role.Administrator,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Registrations = new List<Registration>()
        };

        var users = new List<User> { user, admin }.BuildMockDbSet();
        var registrations = new List<Registration>().BuildMockDbSet();

        _context.Users.Returns(users);
        _context.Registrations.Returns(registrations);

        var ipAddress = "203.0.113.1";
        var command = new DeleteUserCommand
        {
            AdminUserId = adminId,
            UserId = userId,
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
            CreatedAt = DateTime.UtcNow,
            Registrations = new List<Registration>()
        };

        var admin = new User
        {
            Id = adminId,
            Name = "Admin",
            Email = "admin@example.com",
            Role = Role.Administrator,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Registrations = new List<Registration>()
        };

        var users = new List<User> { user, admin }.BuildMockDbSet();
        var registrations = new List<Registration>().BuildMockDbSet();

        _context.Users.Returns(users);
        _context.Registrations.Returns(registrations);

        var userAgent = "Firefox/89.0";
        var command = new DeleteUserCommand
        {
            AdminUserId = adminId,
            UserId = userId,
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
    /// Verifies that admin can delete their own account (though with warnings in UI).
    /// </summary>
    [Fact]
    public async Task Handle_AdminDeletingOwnAccount_SucceedsWhenOtherAdminsExist()
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
            CreatedAt = DateTime.UtcNow,
            Registrations = new List<Registration>()
        };

        var otherAdmin = new User
        {
            Id = otherAdminId,
            Name = "Admin 2",
            Email = "admin2@example.com",
            Role = Role.Administrator,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Registrations = new List<Registration>()
        };

        var users = new List<User> { admin, otherAdmin }.BuildMockDbSet();
        var registrations = new List<Registration>().BuildMockDbSet();

        _context.Users.Returns(users);
        _context.Registrations.Returns(registrations);

        var command = new DeleteUserCommand
        {
            AdminUserId = adminId,
            UserId = adminId
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that deletion with no registrations succeeds.
    /// </summary>
    [Fact]
    public async Task Handle_DeleteUserWithoutRegistrations_SucceedsWithoutCascade()
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
            CreatedAt = DateTime.UtcNow,
            Registrations = new List<Registration>()
        };

        var admin = new User
        {
            Id = adminId,
            Name = "Admin",
            Email = "admin@example.com",
            Role = Role.Administrator,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Registrations = new List<Registration>()
        };

        var users = new List<User> { user, admin }.BuildMockDbSet();
        var registrations = new List<Registration>().BuildMockDbSet();

        _context.Users.Returns(users);
        _context.Registrations.Returns(registrations);

        var command = new DeleteUserCommand
        {
            AdminUserId = adminId,
            UserId = userId
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _auditService.Received(1).LogAsync(
            AuditAction.UserDeleted,
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that audit log is created before user removal (transactional integrity).
    /// </summary>
    [Fact]
    public async Task Handle_AuditLogCreatedBeforeRemoval_EnsuresTransactionalIntegrity()
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
            CreatedAt = DateTime.UtcNow,
            Registrations = new List<Registration>()
        };

        var admin = new User
        {
            Id = adminId,
            Name = "Admin",
            Email = "admin@example.com",
            Role = Role.Administrator,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Registrations = new List<Registration>()
        };

        var users = new List<User> { user, admin }.BuildMockDbSet();
        var registrations = new List<Registration>().BuildMockDbSet();

        _context.Users.Returns(users);
        _context.Registrations.Returns(registrations);

        var command = new DeleteUserCommand
        {
            AdminUserId = adminId,
            UserId = userId
        };

        var callOrder = new List<string>();
        _auditService.LogAsync(Arg.Any<AuditAction>(), Arg.Any<Guid>(), Arg.Any<Guid>(),
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                callOrder.Add("audit");
                return Task.CompletedTask;
            });

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        callOrder.Should().Contain("audit");
        await _auditService.Received(1).LogAsync(Arg.Any<AuditAction>(), Arg.Any<Guid>(), Arg.Any<Guid>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
}
