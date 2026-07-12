using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Application.Registrations.Commands.CancelRegistrationById;
using SportsClubEventManager.Application.Tests.Common;
using SportsClubEventManager.Domain.Entities;
using SportsClubEventManager.Domain.Enums;
using SportsClubEventManager.Domain.Exceptions;
using Xunit;

namespace SportsClubEventManager.Application.Tests.Registrations.Commands.CancelRegistrationById;

/// <summary>
/// Tests for CancelRegistrationByIdCommandHandler, covering both self-service and administrator
/// cancellation paths, including audit logging (issue #45-style pattern) and business-metric
/// recording (issue #42): <c>RecordRegistrationCancelled</c> must be invoked exactly once on the
/// happy path with the correct "source" label ("self-service" or "admin" depending on
/// <see cref="CancelRegistrationByIdCommand.IsAdministrator"/>), and never invoked when the
/// handler throws before reaching <c>SaveChangesAsync</c>.
/// </summary>
public class CancelRegistrationByIdCommandHandlerTests
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CancelRegistrationByIdCommandHandlerTests"/> class.
    /// </summary>
    public CancelRegistrationByIdCommandHandlerTests()
    {
    }

    private static IApplicationMetrics CreateMetrics() => Substitute.For<IApplicationMetrics>();

    private static IAuditService CreateAuditService() => Substitute.For<IAuditService>();

    /// <summary>
    /// Seeds a user, an event and a registration linking them into a fresh in-memory context.
    /// </summary>
    private static (IApplicationDbContext Context, Registration Registration) SeedRegistration(
        DateTime eventDate, RegistrationStatus status = RegistrationStatus.Registered)
    {
        var context = TestDbContextFactory.CreateTestContext();

        var user = new User { Id = Guid.NewGuid(), Name = "Jane Doe", Email = "jane@test.com", Gender = Gender.Female };
        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Swimming Lesson",
            Date = eventDate,
            Location = "Pool 1",
            MaxCapacity = 20
        };
        var registration = new Registration
        {
            Id = Guid.NewGuid(),
            EventId = eventEntity.Id,
            UserId = user.Id,
            Status = status,
            RegistrationDate = DateTime.UtcNow
        };

        context.Users.Add(user);
        context.Events.Add(eventEntity);
        context.Registrations.Add(registration);
        context.SaveChangesAsync(CancellationToken.None).GetAwaiter().GetResult();

        return (context, registration);
    }

    /// <summary>
    /// Tests that verify a user cancelling their own registration (self-service path).
    /// </summary>
    public sealed class WhenSelfServiceCancellationSucceeds : CancelRegistrationByIdCommandHandlerTests
    {
        /// <summary>
        /// Verifies that a user cancelling their own active registration for a future event removes it.
        /// </summary>
        [Fact]
        public async Task Handle_WhenUserCancelsOwnRegistration_RemovesRegistrationFromDatabase()
        {
            // Arrange
            var (context, registration) = SeedRegistration(DateTime.UtcNow.AddDays(7));
            var auditService = CreateAuditService();
            var metrics = CreateMetrics();
            var handler = new CancelRegistrationByIdCommandHandler(context, auditService, metrics);
            var command = new CancelRegistrationByIdCommand
            {
                RegistrationId = registration.Id,
                RequestingUserId = registration.UserId,
                IsAdministrator = false
            };

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            var deleted = await context.Registrations.FirstOrDefaultAsync(r => r.Id == registration.Id);
            deleted.Should().BeNull();
        }

        /// <summary>
        /// Verifies that RecordRegistrationCancelled is invoked exactly once with the "self-service"
        /// label when a non-administrator cancels their own registration.
        /// </summary>
        [Fact]
        public async Task Handle_WhenUserCancelsOwnRegistration_RecordsSelfServiceCancellationMetric()
        {
            // Arrange
            var (context, registration) = SeedRegistration(DateTime.UtcNow.AddDays(7));
            var auditService = CreateAuditService();
            var metrics = CreateMetrics();
            var handler = new CancelRegistrationByIdCommandHandler(context, auditService, metrics);
            var command = new CancelRegistrationByIdCommand
            {
                RegistrationId = registration.Id,
                RequestingUserId = registration.UserId,
                IsAdministrator = false
            };

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            metrics.Received(1).RecordRegistrationCancelled("self-service");
        }

        /// <summary>
        /// Verifies that no audit log entry is created for a self-service cancellation, since
        /// auditing is reserved for administrator-performed actions.
        /// </summary>
        [Fact]
        public async Task Handle_WhenUserCancelsOwnRegistration_DoesNotCreateAuditLog()
        {
            // Arrange
            var (context, registration) = SeedRegistration(DateTime.UtcNow.AddDays(7));
            var auditService = CreateAuditService();
            var metrics = CreateMetrics();
            var handler = new CancelRegistrationByIdCommandHandler(context, auditService, metrics);
            var command = new CancelRegistrationByIdCommand
            {
                RegistrationId = registration.Id,
                RequestingUserId = registration.UserId,
                IsAdministrator = false
            };

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            await auditService.DidNotReceive().LogAsync(
                Arg.Any<AuditAction>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        }
    }

    /// <summary>
    /// Tests that verify an administrator cancelling another user's registration.
    /// </summary>
    public sealed class WhenAdministratorCancellationSucceeds : CancelRegistrationByIdCommandHandlerTests
    {
        /// <summary>
        /// Verifies that an administrator can cancel any user's registration, even for an event
        /// whose date has already occurred (administrators bypass the future-date restriction).
        /// </summary>
        [Fact]
        public async Task Handle_WhenAdministratorCancelsPastEventRegistration_RemovesRegistrationFromDatabase()
        {
            // Arrange
            var (context, registration) = SeedRegistration(DateTime.UtcNow.AddDays(-3));
            var auditService = CreateAuditService();
            var metrics = CreateMetrics();
            var handler = new CancelRegistrationByIdCommandHandler(context, auditService, metrics);
            var command = new CancelRegistrationByIdCommand
            {
                RegistrationId = registration.Id,
                RequestingUserId = Guid.NewGuid(),
                IsAdministrator = true
            };

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            var deleted = await context.Registrations.FirstOrDefaultAsync(r => r.Id == registration.Id);
            deleted.Should().BeNull();
        }

        /// <summary>
        /// Verifies that RecordRegistrationCancelled is invoked exactly once with the "admin" label
        /// when an administrator cancels another user's registration.
        /// </summary>
        [Fact]
        public async Task Handle_WhenAdministratorCancelsRegistration_RecordsAdminCancellationMetric()
        {
            // Arrange
            var (context, registration) = SeedRegistration(DateTime.UtcNow.AddDays(7));
            var auditService = CreateAuditService();
            var metrics = CreateMetrics();
            var handler = new CancelRegistrationByIdCommandHandler(context, auditService, metrics);
            var command = new CancelRegistrationByIdCommand
            {
                RegistrationId = registration.Id,
                RequestingUserId = Guid.NewGuid(),
                IsAdministrator = true
            };

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            metrics.Received(1).RecordRegistrationCancelled("admin");
        }

        /// <summary>
        /// Verifies that an audit log entry is created when an administrator cancels a registration.
        /// </summary>
        [Fact]
        public async Task Handle_WhenAdministratorCancelsRegistration_CreatesAuditLog()
        {
            // Arrange
            var (context, registration) = SeedRegistration(DateTime.UtcNow.AddDays(7));
            var auditService = CreateAuditService();
            var metrics = CreateMetrics();
            var handler = new CancelRegistrationByIdCommandHandler(context, auditService, metrics);
            var adminId = Guid.NewGuid();
            var command = new CancelRegistrationByIdCommand
            {
                RegistrationId = registration.Id,
                RequestingUserId = adminId,
                IsAdministrator = true,
                IpAddress = "192.168.1.10",
                UserAgent = "Mozilla/5.0"
            };

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            await auditService.Received(1).LogAsync(
                AuditAction.RegistrationCancelled,
                adminId,
                registration.UserId,
                "jane@test.com",
                Arg.Any<string>(),
                "192.168.1.10",
                "Mozilla/5.0",
                Arg.Any<CancellationToken>());
        }
    }

    /// <summary>
    /// Tests that verify error handling for a non-existent registration.
    /// </summary>
    public sealed class WhenRegistrationDoesNotExist : CancelRegistrationByIdCommandHandlerTests
    {
        /// <summary>
        /// Verifies that cancelling a non-existent registration throws EntityNotFoundException and
        /// never records a metric or an audit log entry.
        /// </summary>
        [Fact]
        public async Task Handle_WhenRegistrationDoesNotExist_ThrowsEntityNotFoundExceptionAndRecordsNothing()
        {
            // Arrange
            var context = TestDbContextFactory.CreateTestContext();
            var auditService = CreateAuditService();
            var metrics = CreateMetrics();
            var handler = new CancelRegistrationByIdCommandHandler(context, auditService, metrics);
            var registrationId = Guid.NewGuid();
            var command = new CancelRegistrationByIdCommand
            {
                RegistrationId = registrationId,
                RequestingUserId = Guid.NewGuid(),
                IsAdministrator = false
            };

            // Act
            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<EntityNotFoundException>()
                .WithMessage($"Registration with identifier '{registrationId}' does not exist.");
            metrics.DidNotReceive().RecordRegistrationCancelled(Arg.Any<string>());
            await auditService.DidNotReceive().LogAsync(
                Arg.Any<AuditAction>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        }
    }

    /// <summary>
    /// Tests that verify authorization enforcement for non-administrator requests.
    /// </summary>
    public sealed class WhenUnauthorized : CancelRegistrationByIdCommandHandlerTests
    {
        /// <summary>
        /// Verifies that a non-administrator attempting to cancel someone else's registration
        /// throws UnauthorizedAccessException and never records a metric.
        /// </summary>
        [Fact]
        public async Task Handle_WhenNonAdministratorCancelsSomeoneElsesRegistration_ThrowsUnauthorizedAccessExceptionAndRecordsNoMetric()
        {
            // Arrange
            var (context, registration) = SeedRegistration(DateTime.UtcNow.AddDays(7));
            var auditService = CreateAuditService();
            var metrics = CreateMetrics();
            var handler = new CancelRegistrationByIdCommandHandler(context, auditService, metrics);
            var command = new CancelRegistrationByIdCommand
            {
                RegistrationId = registration.Id,
                RequestingUserId = Guid.NewGuid(), // Different from registration.UserId
                IsAdministrator = false
            };

            // Act
            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>();
            metrics.DidNotReceive().RecordRegistrationCancelled(Arg.Any<string>());
        }
    }

    /// <summary>
    /// Tests that verify the future-date restriction for non-administrator requests.
    /// </summary>
    public sealed class WhenEventDateIsInThePast : CancelRegistrationByIdCommandHandlerTests
    {
        /// <summary>
        /// Verifies that a non-administrator cannot cancel a registration for an event that has
        /// already occurred, and that no metric is recorded.
        /// </summary>
        [Fact]
        public async Task Handle_WhenNonAdministratorCancelsPastEventRegistration_ThrowsDomainExceptionAndRecordsNoMetric()
        {
            // Arrange
            var (context, registration) = SeedRegistration(DateTime.UtcNow.AddDays(-1));
            var auditService = CreateAuditService();
            var metrics = CreateMetrics();
            var handler = new CancelRegistrationByIdCommandHandler(context, auditService, metrics);
            var command = new CancelRegistrationByIdCommand
            {
                RegistrationId = registration.Id,
                RequestingUserId = registration.UserId,
                IsAdministrator = false
            };

            // Act
            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("Cannot cancel registrations for events that have already occurred.");
            metrics.DidNotReceive().RecordRegistrationCancelled(Arg.Any<string>());
        }
    }
}
