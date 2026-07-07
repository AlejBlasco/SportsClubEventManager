using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Application.Events.Commands.UpdateEvent;
using SportsClubEventManager.Application.Tests.Common;
using SportsClubEventManager.Domain.Entities;
using Xunit;

namespace SportsClubEventManager.Application.Tests.Events.Commands.UpdateEvent;

/// <summary>
/// Tests for UpdateEventCommandHandler to verify update logic, validation, and audit logging.
/// </summary>
public class UpdateEventCommandHandlerTests
{
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
        var handler = new UpdateEventCommandHandler(context, auditService);

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
        var handler = new UpdateEventCommandHandler(context, auditService);

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
}
