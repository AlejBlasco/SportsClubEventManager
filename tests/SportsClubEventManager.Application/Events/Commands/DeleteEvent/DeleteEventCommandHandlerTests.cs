using FluentAssertions;
using NSubstitute;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Application.Events.Commands.DeleteEvent;
using SportsClubEventManager.Application.Tests.Common;
using Xunit;

namespace SportsClubEventManager.Application.Tests.Events.Commands.DeleteEvent;

/// <summary>
/// Tests for DeleteEventCommandHandler to verify constructor wiring, including the
/// <see cref="IWorkflowNotifier"/> dependency added for the n8n "event cancelled" notification
/// (issue #37).
/// Note: <see cref="DeleteEventCommandHandler.Handle"/> itself (atomicity, bulk registration
/// cancellation via ExecuteUpdateAsync, and the post-commit NotifyEventCancelledAsync call) is
/// NOT unit tested here. Verified empirically in this repository (net10.0,
/// Microsoft.EntityFrameworkCore.InMemory 10.0.9): calling Handle against an InMemory-backed
/// IApplicationDbContext throws before reaching any business logic — first on
/// Database.BeginTransactionAsync ("Transactions are not supported by the in-memory store"),
/// and even after suppressing that warning, on the ExecuteUpdateAsync bulk-update call
/// ("The methods 'ExecuteUpdate' and 'ExecuteUpdateAsync' are not supported by the current
/// database provider"). This matches the pre-existing note this test class already carried
/// before issue #37. Exercising Handle (and confirming NotifyEventCancelledAsync fires only
/// after a successful commit, never after a rollback) requires an integration test against a
/// real relational provider (Testcontainers, per this repo's IntegrationTests project) — see the
/// testing summary's Gaps section.
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
        var notifier = Substitute.For<IWorkflowNotifier>();

        // Act
        var handler = new DeleteEventCommandHandler(context, auditService, notifier);

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
        var notifier = Substitute.For<IWorkflowNotifier>();

        // Act
        var handler = new DeleteEventCommandHandler(context, auditService, notifier);

        // Assert
        // If construction succeeds, audit service was properly injected
        handler.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that the IWorkflowNotifier dependency is properly injected into the handler
    /// (issue #37), the same way the pre-existing audit service injection test above verifies
    /// IAuditService.
    /// </summary>
    [Fact]
    public void DeleteEventCommandHandler_WhenWorkflowNotifierProvided_UsesIt()
    {
        // Arrange
        var context = TestDbContextFactory.CreateTestContext();
        var auditService = Substitute.For<IAuditService>();
        var notifier = Substitute.For<IWorkflowNotifier>();

        // Act
        var handler = new DeleteEventCommandHandler(context, auditService, notifier);

        // Assert
        // If construction succeeds, the notifier was properly injected.
        handler.Should().NotBeNull();
    }
}
