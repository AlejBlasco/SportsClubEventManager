using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Application.Common.Validators;
using SportsClubEventManager.Application.Import.Commands.BulkCreateEvents;
using SportsClubEventManager.Application.Tests.Common;
using SportsClubEventManager.Shared.DTOs;
using Xunit;

namespace SportsClubEventManager.Application.Tests.Import.Commands.BulkCreateEvents;

/// <summary>
/// Tests for BulkCreateEventsCommandHandler to verify all-or-nothing persistence, re-validation, and audit logging.
/// </summary>
public sealed class BulkCreateEventsCommandHandlerTests
{
    private readonly IAuditService _auditService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<BulkCreateEventsCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BulkCreateEventsCommandHandlerTests"/> class.
    /// </summary>
    public BulkCreateEventsCommandHandlerTests()
    {
        _auditService = Substitute.For<IAuditService>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _dateTimeProvider.UtcNow.Returns(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        _logger = Substitute.For<ILogger<BulkCreateEventsCommandHandler>>();
    }

    private BulkCreateEventsCommandHandler CreateHandler(IApplicationDbContext context) =>
        new(context, _auditService, new ImportEventItemDtoValidator(_dateTimeProvider), _logger);

    private static ImportEventItemDto CreateValidItem(string title = "Basketball Tournament") => new()
    {
        Title = title,
        Date = DateTime.UtcNow.AddYears(1),
        Description = "Modality: Trap | Field: Campo 2 | Category: S1",
        Location = "Sports Hall A",
        MaxCapacity = 30
    };

    /// <summary>
    /// Verifies that all valid rows are persisted to the database in a single call.
    /// </summary>
    [Fact]
    public async Task Handle_WhenAllRowsAreValid_CreatesAllEventsInDatabase()
    {
        // Arrange
        var context = TestDbContextFactory.CreateTestContext();
        var handler = CreateHandler(context);
        var command = new BulkCreateEventsCommand
        {
            AdminUserId = Guid.NewGuid(),
            Events = [CreateValidItem("Event A"), CreateValidItem("Event B")]
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.ImportedCount.Should().Be(2);
        result.FailedCount.Should().Be(0);
        (await context.Events.CountAsync()).Should().Be(2);
    }

    /// <summary>
    /// Verifies that exactly one audit log entry is written summarizing the whole import, not one per event.
    /// </summary>
    [Fact]
    public async Task Handle_WhenImportSucceeds_RecordsExactlyOneAuditLogEntry()
    {
        // Arrange
        var context = TestDbContextFactory.CreateTestContext();
        var handler = CreateHandler(context);
        var command = new BulkCreateEventsCommand
        {
            AdminUserId = Guid.NewGuid(),
            Events = [CreateValidItem("Event A"), CreateValidItem("Event B")],
            IpAddress = "127.0.0.1",
            UserAgent = "Test Agent"
        };

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        await _auditService.Received(1).LogAsync(
            Domain.Enums.AuditAction.EventsImported,
            command.AdminUserId,
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            "127.0.0.1",
            "Test Agent",
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that a single invalid row aborts the whole import without persisting any event (all-or-nothing).
    /// </summary>
    [Fact]
    public async Task Handle_WhenAnyRowFailsRevalidation_PersistsNothingAndReturnsFailedRows()
    {
        // Arrange
        var context = TestDbContextFactory.CreateTestContext();
        var handler = CreateHandler(context);
        var invalidItem = CreateValidItem("Bad Event");
        invalidItem.Title = string.Empty;

        var command = new BulkCreateEventsCommand
        {
            AdminUserId = Guid.NewGuid(),
            Events = [CreateValidItem("Good Event"), invalidItem]
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.ImportedCount.Should().Be(0);
        result.FailedCount.Should().Be(1);
        result.FailedRows.Single().RowNumber.Should().Be(2);
        (await context.Events.CountAsync()).Should().Be(0);
    }

    /// <summary>
    /// Verifies that the audit service is never called when the import is rejected due to a failed row.
    /// </summary>
    [Fact]
    public async Task Handle_WhenAnyRowFailsRevalidation_DoesNotCallAuditService()
    {
        // Arrange
        var context = TestDbContextFactory.CreateTestContext();
        var handler = CreateHandler(context);
        var invalidItem = CreateValidItem("Bad Event");
        invalidItem.MaxCapacity = 0;

        var command = new BulkCreateEventsCommand
        {
            AdminUserId = Guid.NewGuid(),
            Events = [invalidItem]
        };

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        await _auditService.DidNotReceive().LogAsync(
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
