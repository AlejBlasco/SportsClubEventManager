using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Application.Events.Commands.CreateEvent;
using SportsClubEventManager.Application.Tests.Common;
using Xunit;

namespace SportsClubEventManager.Application.Tests.Events.Commands.CreateEvent;

/// <summary>
/// Tests for CreateEventCommandHandler to verify event creation, validation, and audit logging.
/// </summary>
public class CreateEventCommandHandlerTests
{
    /// <summary>
    /// Verifies that a valid create command persists the event to the database.
    /// </summary>
    [Fact]
    public async Task Handle_WhenCommandIsValid_CreatesEventInDatabase()
    {
        // Arrange
        var context = TestDbContextFactory.CreateTestContext();
        var auditService = Substitute.For<IAuditService>();
        var handler = new CreateEventCommandHandler(context, auditService);

        var futureDate = DateTime.UtcNow.AddDays(7);
        var command = new CreateEventCommand
        {
            Title = "Basketball Tournament",
            Date = futureDate,
            Location = "Sports Hall A",
            MaxCapacity = 100,
            AdminUserId = Guid.NewGuid(),
            IpAddress = "127.0.0.1",
            UserAgent = "Test Agent"
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBe(Guid.Empty);
        var createdEvent = await context.Events.FirstOrDefaultAsync(e => e.Id == result);
        createdEvent.Should().NotBeNull();
        createdEvent!.Title.Should().Be("Basketball Tournament");
        createdEvent.Location.Should().Be("Sports Hall A");
    }

    /// <summary>
    /// Verifies that audit service is called after event creation.
    /// </summary>
    [Fact]
    public async Task Handle_WhenEventIsCreated_AuditLogIsRecorded()
    {
        // Arrange
        var context = TestDbContextFactory.CreateTestContext();
        var auditService = Substitute.For<IAuditService>();
        var handler = new CreateEventCommandHandler(context, auditService);

        var command = new CreateEventCommand
        {
            Title = "Test Event",
            Date = DateTime.UtcNow.AddDays(1),
            Location = "Test Location",
            MaxCapacity = 50,
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
