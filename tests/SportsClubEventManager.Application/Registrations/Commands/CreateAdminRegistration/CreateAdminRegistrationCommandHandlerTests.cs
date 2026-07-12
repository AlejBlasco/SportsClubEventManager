using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Application.Registrations.Commands.CreateAdminRegistration;
using SportsClubEventManager.Application.Tests.Common;
using SportsClubEventManager.Domain.Entities;
using SportsClubEventManager.Domain.Enums;
using SportsClubEventManager.Domain.Exceptions;
using Xunit;

namespace SportsClubEventManager.Application.Tests.Registrations.Commands.CreateAdminRegistration;

/// <summary>
/// Tests for CreateAdminRegistrationCommandHandler, covering administrator-initiated registration
/// creation, audit logging, and business-metric recording (issue #42):
/// <c>RecordRegistrationCreated("admin")</c> must be invoked exactly once on the happy path, after
/// a successful <c>SaveChangesAsync</c>, and never invoked when the handler throws beforehand.
/// </summary>
public class CreateAdminRegistrationCommandHandlerTests
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateAdminRegistrationCommandHandlerTests"/> class.
    /// </summary>
    public CreateAdminRegistrationCommandHandlerTests()
    {
    }

    private static IApplicationMetrics CreateMetrics() => Substitute.For<IApplicationMetrics>();

    private static IAuditService CreateAuditService() => Substitute.For<IAuditService>();

    private static User CreateActiveUser(string email = "member@test.com") => new()
    {
        Id = Guid.NewGuid(),
        Name = "Active Member",
        Email = email,
        Gender = Gender.Male,
        IsActive = true
    };

    private static Event CreateEvent(DateTime date, int maxCapacity, List<Registration>? registrations = null) => new()
    {
        Id = Guid.NewGuid(),
        Title = "Basketball Tournament",
        Date = date,
        Location = "Sports Hall A",
        MaxCapacity = maxCapacity,
        Registrations = registrations ?? new List<Registration>()
    };

    /// <summary>
    /// Tests that verify successful admin registration creation.
    /// </summary>
    public sealed class WhenRegistrationIsSuccessful : CreateAdminRegistrationCommandHandlerTests
    {
        /// <summary>
        /// Verifies that a valid admin registration request persists a new registration and
        /// returns its identifier.
        /// </summary>
        [Fact]
        public async Task Handle_WhenValidRequest_PersistsRegistrationAndReturnsId()
        {
            // Arrange
            var user = CreateActiveUser();
            var eventEntity = CreateEvent(DateTime.UtcNow.AddDays(7), 100);

            var context = TestDbContextFactory.CreateTestContext();
            context.Users.Add(user);
            context.Events.Add(eventEntity);
            await context.SaveChangesAsync(CancellationToken.None);

            var auditService = CreateAuditService();
            var metrics = CreateMetrics();
            var handler = new CreateAdminRegistrationCommandHandler(context, auditService, metrics);
            var command = new CreateAdminRegistrationCommand
            {
                AdminUserId = Guid.NewGuid(),
                UserId = user.Id,
                EventId = eventEntity.Id
            };

            // Act
            var registrationId = await handler.Handle(command, CancellationToken.None);

            // Assert
            registrationId.Should().NotBeEmpty();
            var persisted = await context.Registrations.FirstOrDefaultAsync(r => r.Id == registrationId);
            persisted.Should().NotBeNull();
            persisted!.EventId.Should().Be(eventEntity.Id);
            persisted.UserId.Should().Be(user.Id);
            persisted.Status.Should().Be(RegistrationStatus.Registered);
        }

        /// <summary>
        /// Verifies that RecordRegistrationCreated is invoked exactly once with the "admin" label
        /// after a successful registration.
        /// </summary>
        [Fact]
        public async Task Handle_WhenValidRequest_RecordsAdminRegistrationCreatedMetric()
        {
            // Arrange
            var user = CreateActiveUser();
            var eventEntity = CreateEvent(DateTime.UtcNow.AddDays(7), 100);

            var context = TestDbContextFactory.CreateTestContext();
            context.Users.Add(user);
            context.Events.Add(eventEntity);
            await context.SaveChangesAsync(CancellationToken.None);

            var auditService = CreateAuditService();
            var metrics = CreateMetrics();
            var handler = new CreateAdminRegistrationCommandHandler(context, auditService, metrics);
            var command = new CreateAdminRegistrationCommand
            {
                AdminUserId = Guid.NewGuid(),
                UserId = user.Id,
                EventId = eventEntity.Id
            };

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            metrics.Received(1).RecordRegistrationCreated("admin");
        }

        /// <summary>
        /// Verifies that an audit log entry is created capturing the acting administrator and the
        /// target user/event details.
        /// </summary>
        [Fact]
        public async Task Handle_WhenValidRequest_CreatesAuditLog()
        {
            // Arrange
            var user = CreateActiveUser("target@test.com");
            var eventEntity = CreateEvent(DateTime.UtcNow.AddDays(7), 100);

            var context = TestDbContextFactory.CreateTestContext();
            context.Users.Add(user);
            context.Events.Add(eventEntity);
            await context.SaveChangesAsync(CancellationToken.None);

            var auditService = CreateAuditService();
            var metrics = CreateMetrics();
            var handler = new CreateAdminRegistrationCommandHandler(context, auditService, metrics);
            var adminId = Guid.NewGuid();
            var command = new CreateAdminRegistrationCommand
            {
                AdminUserId = adminId,
                UserId = user.Id,
                EventId = eventEntity.Id,
                IpAddress = "10.0.0.5",
                UserAgent = "Mozilla/5.0"
            };

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            await auditService.Received(1).LogAsync(
                AuditAction.RegistrationCreated,
                adminId,
                user.Id,
                "target@test.com",
                Arg.Any<string>(),
                "10.0.0.5",
                "Mozilla/5.0",
                Arg.Any<CancellationToken>());
        }
    }

    /// <summary>
    /// Tests that verify error handling for a non-existent event.
    /// </summary>
    public sealed class WhenEventDoesNotExist : CreateAdminRegistrationCommandHandlerTests
    {
        /// <summary>
        /// Verifies that attempting to register for a non-existent event throws
        /// EntityNotFoundException and never records a metric.
        /// </summary>
        [Fact]
        public async Task Handle_WhenEventDoesNotExist_ThrowsEntityNotFoundExceptionAndRecordsNoMetric()
        {
            // Arrange
            var context = TestDbContextFactory.CreateTestContext();
            var auditService = CreateAuditService();
            var metrics = CreateMetrics();
            var handler = new CreateAdminRegistrationCommandHandler(context, auditService, metrics);
            var eventId = Guid.NewGuid();
            var command = new CreateAdminRegistrationCommand
            {
                AdminUserId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                EventId = eventId
            };

            // Act
            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<EntityNotFoundException>()
                .WithMessage($"Event with identifier '{eventId}' does not exist.");
            metrics.DidNotReceive().RecordRegistrationCreated(Arg.Any<string>());
        }
    }

    /// <summary>
    /// Tests that verify the future-date restriction.
    /// </summary>
    public sealed class WhenEventDateIsInThePast : CreateAdminRegistrationCommandHandlerTests
    {
        /// <summary>
        /// Verifies that registering a user for a past event throws DomainException and never
        /// records a metric.
        /// </summary>
        [Fact]
        public async Task Handle_WhenEventDateIsInPast_ThrowsDomainExceptionAndRecordsNoMetric()
        {
            // Arrange
            var eventEntity = CreateEvent(DateTime.UtcNow.AddDays(-1), 50);

            var context = TestDbContextFactory.CreateTestContext();
            context.Events.Add(eventEntity);
            await context.SaveChangesAsync(CancellationToken.None);

            var auditService = CreateAuditService();
            var metrics = CreateMetrics();
            var handler = new CreateAdminRegistrationCommandHandler(context, auditService, metrics);
            var command = new CreateAdminRegistrationCommand
            {
                AdminUserId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                EventId = eventEntity.Id
            };

            // Act
            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("Cannot register users for events that have already occurred.");
            metrics.DidNotReceive().RecordRegistrationCreated(Arg.Any<string>());
        }
    }

    /// <summary>
    /// Tests that verify error handling for a non-existent target user.
    /// </summary>
    public sealed class WhenUserDoesNotExist : CreateAdminRegistrationCommandHandlerTests
    {
        /// <summary>
        /// Verifies that registering a non-existent user throws EntityNotFoundException and never
        /// records a metric.
        /// </summary>
        [Fact]
        public async Task Handle_WhenUserDoesNotExist_ThrowsEntityNotFoundExceptionAndRecordsNoMetric()
        {
            // Arrange
            var eventEntity = CreateEvent(DateTime.UtcNow.AddDays(7), 50);

            var context = TestDbContextFactory.CreateTestContext();
            context.Events.Add(eventEntity);
            await context.SaveChangesAsync(CancellationToken.None);

            var auditService = CreateAuditService();
            var metrics = CreateMetrics();
            var handler = new CreateAdminRegistrationCommandHandler(context, auditService, metrics);
            var userId = Guid.NewGuid();
            var command = new CreateAdminRegistrationCommand
            {
                AdminUserId = Guid.NewGuid(),
                UserId = userId,
                EventId = eventEntity.Id
            };

            // Act
            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<EntityNotFoundException>()
                .WithMessage($"User with identifier '{userId}' does not exist.");
            metrics.DidNotReceive().RecordRegistrationCreated(Arg.Any<string>());
        }
    }

    /// <summary>
    /// Tests that verify inactive users cannot be registered.
    /// </summary>
    public sealed class WhenUserIsInactive : CreateAdminRegistrationCommandHandlerTests
    {
        /// <summary>
        /// Verifies that registering an inactive user throws InvalidOperationException and never
        /// records a metric.
        /// </summary>
        [Fact]
        public async Task Handle_WhenUserIsInactive_ThrowsInvalidOperationExceptionAndRecordsNoMetric()
        {
            // Arrange
            var inactiveUser = CreateActiveUser();
            inactiveUser.IsActive = false;
            var eventEntity = CreateEvent(DateTime.UtcNow.AddDays(7), 50);

            var context = TestDbContextFactory.CreateTestContext();
            context.Users.Add(inactiveUser);
            context.Events.Add(eventEntity);
            await context.SaveChangesAsync(CancellationToken.None);

            var auditService = CreateAuditService();
            var metrics = CreateMetrics();
            var handler = new CreateAdminRegistrationCommandHandler(context, auditService, metrics);
            var command = new CreateAdminRegistrationCommand
            {
                AdminUserId = Guid.NewGuid(),
                UserId = inactiveUser.Id,
                EventId = eventEntity.Id
            };

            // Act
            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>();
            metrics.DidNotReceive().RecordRegistrationCreated(Arg.Any<string>());
        }
    }

    /// <summary>
    /// Tests that verify duplicate-registration handling.
    /// </summary>
    public sealed class WhenUserAlreadyRegistered : CreateAdminRegistrationCommandHandlerTests
    {
        /// <summary>
        /// Verifies that registering a user who already has an active registration throws
        /// DuplicateRegistrationException and never records a metric.
        /// </summary>
        [Fact]
        public async Task Handle_WhenUserAlreadyHasActiveRegistration_ThrowsDuplicateRegistrationExceptionAndRecordsNoMetric()
        {
            // Arrange
            var user = CreateActiveUser();
            var existingRegistration = new Registration
            {
                UserId = user.Id,
                Status = RegistrationStatus.Registered
            };
            var eventEntity = CreateEvent(DateTime.UtcNow.AddDays(7), 50, new List<Registration> { existingRegistration });

            var context = TestDbContextFactory.CreateTestContext();
            context.Users.Add(user);
            context.Events.Add(eventEntity);
            await context.SaveChangesAsync(CancellationToken.None);

            var auditService = CreateAuditService();
            var metrics = CreateMetrics();
            var handler = new CreateAdminRegistrationCommandHandler(context, auditService, metrics);
            var command = new CreateAdminRegistrationCommand
            {
                AdminUserId = Guid.NewGuid(),
                UserId = user.Id,
                EventId = eventEntity.Id
            };

            // Act
            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<DuplicateRegistrationException>()
                .WithMessage("User is already registered for this event.");
            metrics.DidNotReceive().RecordRegistrationCreated(Arg.Any<string>());
        }
    }

    /// <summary>
    /// Tests that verify capacity enforcement.
    /// </summary>
    public sealed class WhenEventIsAtFullCapacity : CreateAdminRegistrationCommandHandlerTests
    {
        /// <summary>
        /// Verifies that registering a user for an event at maximum capacity throws
        /// CapacityExceededException and never records a metric.
        /// </summary>
        [Fact]
        public async Task Handle_WhenEventIsAtMaxCapacity_ThrowsCapacityExceededExceptionAndRecordsNoMetric()
        {
            // Arrange
            var user = CreateActiveUser();
            const int maxCapacity = 3;
            var existingRegistrations = Enumerable.Range(0, maxCapacity)
                .Select(_ => new Registration { UserId = Guid.NewGuid(), Status = RegistrationStatus.Registered })
                .ToList();
            var eventEntity = CreateEvent(DateTime.UtcNow.AddDays(7), maxCapacity, existingRegistrations);

            var context = TestDbContextFactory.CreateTestContext();
            context.Users.Add(user);
            context.Events.Add(eventEntity);
            await context.SaveChangesAsync(CancellationToken.None);

            var auditService = CreateAuditService();
            var metrics = CreateMetrics();
            var handler = new CreateAdminRegistrationCommandHandler(context, auditService, metrics);
            var command = new CreateAdminRegistrationCommand
            {
                AdminUserId = Guid.NewGuid(),
                UserId = user.Id,
                EventId = eventEntity.Id
            };

            // Act
            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<CapacityExceededException>()
                .WithMessage("Event has reached maximum capacity.");
            metrics.DidNotReceive().RecordRegistrationCreated(Arg.Any<string>());
        }
    }
}
