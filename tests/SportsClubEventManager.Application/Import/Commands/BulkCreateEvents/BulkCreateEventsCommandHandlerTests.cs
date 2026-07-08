using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Application.Common.Validators;
using SportsClubEventManager.Application.Import.Commands.BulkCreateEvents;
using SportsClubEventManager.Application.Import.Services;
using SportsClubEventManager.Application.Tests.Common;
using SportsClubEventManager.Shared.DTOs;
using Xunit;

namespace SportsClubEventManager.Application.Tests.Import.Commands.BulkCreateEvents;

/// <summary>
/// Tests for BulkCreateEventsCommandHandler to verify all-or-nothing persistence, re-validation, and audit logging.
/// The real <see cref="EventImportValidationService"/> is used (against the same in-memory context the handler
/// persists to) so re-validation, including duplicate detection against already-persisted events, is exercised
/// end-to-end.
/// </summary>
public sealed class BulkCreateEventsCommandHandlerTests
{
    private readonly IAuditService _auditService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BulkCreateEventsCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BulkCreateEventsCommandHandlerTests"/> class.
    /// </summary>
    public BulkCreateEventsCommandHandlerTests()
    {
        _auditService = Substitute.For<IAuditService>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _dateTimeProvider.UtcNow.Returns(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        _configuration = new ConfigurationBuilder().Build();
        _logger = Substitute.For<ILogger<BulkCreateEventsCommandHandler>>();
    }

    private BulkCreateEventsCommandHandler CreateHandler(IApplicationDbContext context)
    {
        var validationService = new EventImportValidationService(
            context,
            new ImportEventItemDtoValidator(_dateTimeProvider),
            _configuration,
            Substitute.For<ILogger<EventImportValidationService>>());

        return new BulkCreateEventsCommandHandler(context, _auditService, validationService, _logger);
    }

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

    /// <summary>
    /// Verifies that two submitted rows sharing the same (title, date) key are both rejected as
    /// duplicates by the real validation service's intra-batch detection, aborting the whole
    /// import (all-or-nothing) and persisting nothing, exercising the handler's end-to-end wiring
    /// into the new duplicate-detection path (not just its pre-existing field-validation path).
    /// </summary>
    [Fact]
    public async Task Handle_WhenTwoRowsShareTitleAndDate_RejectsBothAsDuplicatesAndPersistsNothing()
    {
        // Arrange
        var context = TestDbContextFactory.CreateTestContext();
        var handler = CreateHandler(context);
        var sharedDate = DateTime.UtcNow.AddYears(1);
        var firstItem = CreateValidItem("Trap Shooting");
        firstItem.Date = sharedDate;
        var secondItem = CreateValidItem("TRAP SHOOTING");
        secondItem.Date = sharedDate;
        var command = new BulkCreateEventsCommand
        {
            AdminUserId = Guid.NewGuid(),
            Events = [firstItem, secondItem]
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.ImportedCount.Should().Be(0);
        result.FailedCount.Should().Be(1);
        result.FailedRows.Single().RowNumber.Should().Be(2);
        result.FailedRows.Single().IsDuplicate.Should().BeTrue();
        (await context.Events.CountAsync()).Should().Be(0);
    }

    /// <summary>
    /// Verifies that when the import succeeds, the persisted <c>Event.Title</c> reflects the
    /// validation service's normalization (trimmed and title-cased), confirming the handler
    /// builds the entities to persist from <c>NormalizedItem</c> rather than the raw submitted title.
    /// </summary>
    [Fact]
    public async Task Handle_WhenImportSucceeds_PersistsNormalizedTitle()
    {
        // Arrange
        var context = TestDbContextFactory.CreateTestContext();
        var handler = CreateHandler(context);
        var command = new BulkCreateEventsCommand
        {
            AdminUserId = Guid.NewGuid(),
            Events = [CreateValidItem("  trap shooting session  ")]
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.ImportedCount.Should().Be(1);
        var persistedEvent = (await context.Events.ToListAsync()).Single();
        persistedEvent.Title.Should().Be("Trap Shooting Session");
    }
}
