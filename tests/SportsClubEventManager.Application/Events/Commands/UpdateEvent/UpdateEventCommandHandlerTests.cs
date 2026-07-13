using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Application.Common.Models.Notifications;
using SportsClubEventManager.Application.Events.Commands.UpdateEvent;
using SportsClubEventManager.Application.Tests.Common;
using SportsClubEventManager.Domain.Entities;
using SportsClubEventManager.Domain.Enums;
using SportsClubEventManager.Domain.Exceptions;
using Xunit;

namespace SportsClubEventManager.Application.Tests.Events.Commands.UpdateEvent;

/// <summary>
/// Tests for UpdateEventCommandHandler to verify update logic, validation, audit logging, and
/// the n8n "event updated" notification triggered after a successful update (issue #37).
/// </summary>
public class UpdateEventCommandHandlerTests
{
    /// <summary>
    /// Creates a fresh IWorkflowNotifier substitute for a single test, so notification
    /// invocations asserted in one test can never leak into another (issue #37).
    /// </summary>
    /// <returns>A substitute for <see cref="IWorkflowNotifier"/>.</returns>
    private static IWorkflowNotifier CreateNotifier() => Substitute.For<IWorkflowNotifier>();

    /// <summary>
    /// Verifies that a valid update command persists changes to the database.
    /// </summary>
    [Fact]
    public async Task Handle_WhenCommandIsValid_UpdatesEventInDatabase()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var futureDate = DateTime.UtcNow.AddDays(7);
        var newDate = DateTime.UtcNow.AddDays(14);

        var existingEvent = new Event
        {
            Id = eventId,
            Title = "Original Title",
            Date = futureDate,
            Location = "Original Location",
            MaxCapacity = 100
        };

        var context = TestDbContextFactory.CreateTestContextWithEvents(new List<Event> { existingEvent });
        var auditService = Substitute.For<IAuditService>();
        var notifier = CreateNotifier();
        var handler = new UpdateEventCommandHandler(context, auditService, notifier);

        var command = new UpdateEventCommand
        {
            EventId = eventId,
            Title = "Updated Title",
            Date = newDate,
            Location = "Updated Location",
            MaxCapacity = 150,
            AdminUserId = Guid.NewGuid(),
            IpAddress = "127.0.0.1",
            UserAgent = "Test Agent"
        };

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var updatedEvent = await context.Events.FirstOrDefaultAsync(e => e.Id == eventId);
        updatedEvent.Should().NotBeNull();
        updatedEvent!.Title.Should().Be("Updated Title");
        updatedEvent.Location.Should().Be("Updated Location");
        updatedEvent.MaxCapacity.Should().Be(150);
    }

    /// <summary>
    /// Verifies that audit service is called after update.
    /// </summary>
    [Fact]
    public async Task Handle_WhenEventIsUpdated_AuditLogIsRecorded()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var futureDate = DateTime.UtcNow.AddDays(7);

        var existingEvent = new Event
        {
            Id = eventId,
            Title = "Original Title",
            Date = futureDate,
            Location = "Location",
            MaxCapacity = 100
        };

        var context = TestDbContextFactory.CreateTestContextWithEvents(new List<Event> { existingEvent });
        var auditService = Substitute.For<IAuditService>();
        var notifier = CreateNotifier();
        var handler = new UpdateEventCommandHandler(context, auditService, notifier);

        var command = new UpdateEventCommand
        {
            EventId = eventId,
            Title = "Updated Title",
            Date = futureDate,
            Location = "Location",
            MaxCapacity = 100,
            AdminUserId = Guid.NewGuid(),
            IpAddress = "127.0.0.1",
            UserAgent = "Test Agent"
        };

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        await auditService.Received(1).LogAsync(
            Arg.Any<Domain.Enums.AuditAction>(),
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Tests that verify the n8n "event updated" notification triggered after a successful
    /// update (issue #37).
    /// </summary>
    public sealed class WhenNotifyingEventUpdated
    {
        /// <summary>
        /// Verifies that the notifier is invoked exactly once with ChangeType "updated" and a
        /// payload matching the updated event's details.
        /// </summary>
        [Fact]
        public async Task Handle_WhenEventIsUpdated_NotifiesWithUpdatedChangeTypeAndMatchingDetails()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var newDate = DateTime.UtcNow.AddDays(14);
            var existingEvent = new Event
            {
                Id = eventId,
                Title = "Original Title",
                Date = DateTime.UtcNow.AddDays(7),
                Location = "Original Location",
                MaxCapacity = 100
            };

            var context = TestDbContextFactory.CreateTestContextWithEvents(new List<Event> { existingEvent });
            var auditService = Substitute.For<IAuditService>();
            var notifier = CreateNotifier();
            var handler = new UpdateEventCommandHandler(context, auditService, notifier);

            var command = new UpdateEventCommand
            {
                EventId = eventId,
                Title = "Updated Title",
                Date = newDate,
                Location = "Updated Location",
                MaxCapacity = 150,
                AdminUserId = Guid.NewGuid(),
                IpAddress = "127.0.0.1",
                UserAgent = "Test Agent"
            };

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            await notifier.Received(1).NotifyEventUpdatedAsync(
                Arg.Is<EventChangedPayload>(p =>
                    p.EventId == eventId &&
                    p.EventTitle == "Updated Title" &&
                    p.EventDate == newDate &&
                    p.Location == "Updated Location" &&
                    p.ChangeType == "updated"),
                Arg.Any<CancellationToken>());
        }

        /// <summary>
        /// Verifies that only actively registered users (excluding cancelled registrations) are
        /// included as recipients in the notification payload.
        /// </summary>
        [Fact]
        public async Task Handle_WhenSomeRegistrationsAreCancelled_ExcludesThemFromRecipients()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var activeUser = new User { Name = "Active User", Email = "active@example.com", Gender = Gender.Female };
            var cancelledUser = new User { Name = "Cancelled User", Email = "cancelled@example.com", Gender = Gender.Female };

            var existingEvent = new Event
            {
                Id = eventId,
                Title = "Original Title",
                Date = DateTime.UtcNow.AddDays(7),
                Location = "Location",
                MaxCapacity = 100,
                Registrations = new List<Registration>
                {
                    new() { User = activeUser, UserId = activeUser.Id, Status = RegistrationStatus.Registered },
                    new() { User = cancelledUser, UserId = cancelledUser.Id, Status = RegistrationStatus.Cancelled }
                }
            };

            var context = TestDbContextFactory.CreateTestContextWithEvents(new List<Event> { existingEvent });

            var auditService = Substitute.For<IAuditService>();
            var notifier = CreateNotifier();
            var handler = new UpdateEventCommandHandler(context, auditService, notifier);

            var command = new UpdateEventCommand
            {
                EventId = eventId,
                Title = "Original Title",
                Date = existingEvent.Date,
                Location = "Location",
                MaxCapacity = 100,
                AdminUserId = Guid.NewGuid(),
                IpAddress = "127.0.0.1",
                UserAgent = "Test Agent"
            };

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            await notifier.Received(1).NotifyEventUpdatedAsync(
                Arg.Is<EventChangedPayload>(p =>
                    p.Recipients.Count == 1 && p.Recipients[0].Email == activeUser.Email),
                Arg.Any<CancellationToken>());
        }

        /// <summary>
        /// Verifies that the notifier is never invoked when the event does not exist, since the
        /// handler throws before reaching the point where a notification would be sent.
        /// </summary>
        [Fact]
        public async Task Handle_WhenEventDoesNotExist_DoesNotNotify()
        {
            // Arrange
            var context = TestDbContextFactory.CreateTestContext();
            var auditService = Substitute.For<IAuditService>();
            var notifier = CreateNotifier();
            var handler = new UpdateEventCommandHandler(context, auditService, notifier);

            var command = new UpdateEventCommand
            {
                EventId = Guid.NewGuid(),
                Title = "Title",
                Date = DateTime.UtcNow.AddDays(7),
                Location = "Location",
                MaxCapacity = 50,
                AdminUserId = Guid.NewGuid(),
                IpAddress = "127.0.0.1",
                UserAgent = "Test Agent"
            };

            // Act
            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>();
            await notifier.DidNotReceive().NotifyEventUpdatedAsync(
                Arg.Any<EventChangedPayload>(), Arg.Any<CancellationToken>());
        }

        /// <summary>
        /// Verifies that the notifier is never invoked when the event is in the past, since
        /// past events are read-only and the handler throws before persisting any change.
        /// </summary>
        [Fact]
        public async Task Handle_WhenEventIsInThePast_DoesNotNotify()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var existingEvent = new Event
            {
                Id = eventId,
                Title = "Past Event",
                Date = DateTime.UtcNow.AddDays(-1),
                Location = "Location",
                MaxCapacity = 50
            };

            var context = TestDbContextFactory.CreateTestContextWithEvents(new List<Event> { existingEvent });
            var auditService = Substitute.For<IAuditService>();
            var notifier = CreateNotifier();
            var handler = new UpdateEventCommandHandler(context, auditService, notifier);

            var command = new UpdateEventCommand
            {
                EventId = eventId,
                Title = "Past Event",
                Date = DateTime.UtcNow.AddDays(-1),
                Location = "Location",
                MaxCapacity = 50,
                AdminUserId = Guid.NewGuid(),
                IpAddress = "127.0.0.1",
                UserAgent = "Test Agent"
            };

            // Act
            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<DomainException>();
            await notifier.DidNotReceive().NotifyEventUpdatedAsync(
                Arg.Any<EventChangedPayload>(), Arg.Any<CancellationToken>());
        }
    }
}
