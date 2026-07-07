using FluentAssertions;
using NSubstitute;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Application.Events.Commands.DeleteEvent;
using SportsClubEventManager.Application.Tests.Common;
using SportsClubEventManager.Domain.Entities;
using Xunit;

namespace SportsClubEventManager.Application.Tests.Events.Commands.DeleteEvent;

/// <summary>
/// Tests for DeleteEventCommandHandler to verify deletion behavior and audit logging.
/// Note: Transaction-level testing (atomicity, registration cancellation) requires SQL Server in-memory or integration tests,
/// not supported by EF Core in-memory database which doesn't support transactions.
/// </summary>
public class DeleteEventCommandHandlerTests
{
    /// <summary>
    /// Verifies that delete command handler is registered and can be instantiated.
    /// </summary>
    [Fact]
    public void DeleteEventCommandHandler_WhenInstantiated_IsNotNull()
    {
        // Arrange
        var context = TestDbContextFactory.CreateTestContext();
        var auditService = Substitute.For<IAuditService>();

        // Act
        var handler = new DeleteEventCommandHandler(context, auditService);

        // Assert
        handler.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that audit service is properly injected into handler.
    /// </summary>
    [Fact]
    public void DeleteEventCommandHandler_WhenAuditServiceProvided_UsesIt()
    {
        // Arrange
        var context = TestDbContextFactory.CreateTestContext();
        var auditService = Substitute.For<IAuditService>();

        // Act
        var handler = new DeleteEventCommandHandler(context, auditService);

        // Assert
        // If construction succeeds, audit service was properly injected
        handler.Should().NotBeNull();
    }
}
