using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SportsClubEventManager.Application.Common.Constants;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Application.Common.Validators;
using SportsClubEventManager.Application.Import.Services;
using SportsClubEventManager.Application.Tests.Common;
using SportsClubEventManager.Domain.Entities;
using SportsClubEventManager.Shared.DTOs;
using Xunit;

namespace SportsClubEventManager.Application.Tests.Import.Services;

/// <summary>
/// Tests for EventImportValidationService, covering title/location normalization and both
/// levels of duplicate detection (intra-batch and against already-persisted events).
/// </summary>
public sealed class EventImportValidationServiceTests
{
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<EventImportValidationService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventImportValidationServiceTests"/> class.
    /// </summary>
    public EventImportValidationServiceTests()
    {
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _dateTimeProvider.UtcNow.Returns(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        _logger = Substitute.For<ILogger<EventImportValidationService>>();
    }

    private EventImportValidationService CreateService(IApplicationDbContext context, bool? normalizeTitleCapitalization = null)
    {
        var configurationValues = new Dictionary<string, string?>();

        if (normalizeTitleCapitalization.HasValue)
        {
            configurationValues[ImportSettingsKeys.NormalizeTitleCapitalization] = normalizeTitleCapitalization.Value.ToString();
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationValues)
            .Build();

        return new EventImportValidationService(context, new ImportEventItemDtoValidator(_dateTimeProvider), configuration, _logger);
    }

    private static ImportEventItemDto CreateItem(
        string title = "Trap Shooting Session",
        DateTime? date = null,
        string location = "Range 1") => new()
        {
            Title = title,
            Date = date ?? new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc),
            Description = "Modality: Trap | Field: Campo 2 | Category: S1",
            Location = location,
            MaxCapacity = 30
        };

    /// <summary>
    /// Verifies that, with title capitalization normalization enabled (the default), a trimmed,
    /// lowercase-ish title is converted to title case.
    /// </summary>
    [Fact]
    public async Task ValidateAsync_WhenNormalizeTitleCapitalizationEnabled_TitleCasesTrimmedTitle()
    {
        // Arrange
        var context = TestDbContextFactory.CreateTestContext();
        var service = CreateService(context, normalizeTitleCapitalization: true);
        var candidate = CreateItem(title: "  the annual sports tournament  ");

        // Act
        var results = await service.ValidateAsync([candidate], CancellationToken.None);

        // Assert
        results.Single().NormalizedItem.Title.Should().Be("The Annual Sports Tournament");
    }

    /// <summary>
    /// Verifies that, with title capitalization normalization disabled, the title is trimmed but
    /// its original casing is otherwise preserved.
    /// </summary>
    [Fact]
    public async Task ValidateAsync_WhenNormalizeTitleCapitalizationDisabled_OnlyTrimsTitle()
    {
        // Arrange
        var context = TestDbContextFactory.CreateTestContext();
        var service = CreateService(context, normalizeTitleCapitalization: false);
        var candidate = CreateItem(title: "  the ANNUAL Sports tournament  ");

        // Act
        var results = await service.ValidateAsync([candidate], CancellationToken.None);

        // Assert
        results.Single().NormalizedItem.Title.Should().Be("the ANNUAL Sports tournament");
    }

    /// <summary>
    /// Verifies that the location is trimmed regardless of the title capitalization setting.
    /// </summary>
    [Fact]
    public async Task ValidateAsync_TrimsLocationWhitespace()
    {
        // Arrange
        var context = TestDbContextFactory.CreateTestContext();
        var service = CreateService(context);
        var candidate = CreateItem(location: "  Main Hall  ");

        // Act
        var results = await service.ValidateAsync([candidate], CancellationToken.None);

        // Assert
        results.Single().NormalizedItem.Location.Should().Be("Main Hall");
    }

    /// <summary>
    /// Verifies that, when two rows share the same normalized title and exact date/time, the
    /// first row is kept valid and the second is marked as a duplicate of the first.
    /// </summary>
    [Fact]
    public async Task ValidateAsync_WhenTwoRowsShareExactKey_MarksSecondAsDuplicateOfFirst()
    {
        // Arrange
        var context = TestDbContextFactory.CreateTestContext();
        var service = CreateService(context);
        var sharedDate = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);
        var candidates = new List<ImportEventItemDto>
        {
            CreateItem(title: "Trap Shooting", date: sharedDate),
            CreateItem(title: "TRAP SHOOTING", date: sharedDate)
        };

        // Act
        var results = await service.ValidateAsync(candidates, CancellationToken.None);

        // Assert
        results[0].IsDuplicate.Should().BeFalse();
        results[0].IsValid.Should().BeTrue();
        results[1].IsDuplicate.Should().BeTrue();
        results[1].IsValid.Should().BeFalse();
        results[1].Errors.Should().Contain("Duplicate of row 1");
    }

    /// <summary>
    /// Verifies that, when three rows share the same key, the first is kept valid and both
    /// later rows are reported as duplicates of the first row (not of each other).
    /// </summary>
    [Fact]
    public async Task ValidateAsync_WhenThreeRowsShareExactKey_KeepsFirstAndMarksOthersAsDuplicatesOfIt()
    {
        // Arrange
        var context = TestDbContextFactory.CreateTestContext();
        var service = CreateService(context);
        var sharedDate = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);
        var candidates = new List<ImportEventItemDto>
        {
            CreateItem(title: "Trap Shooting", date: sharedDate),
            CreateItem(title: "Trap Shooting", date: sharedDate),
            CreateItem(title: "Trap Shooting", date: sharedDate)
        };

        // Act
        var results = await service.ValidateAsync(candidates, CancellationToken.None);

        // Assert
        results[0].IsDuplicate.Should().BeFalse();
        results[1].Errors.Should().Contain("Duplicate of row 1");
        results[2].Errors.Should().Contain("Duplicate of row 1");
    }

    /// <summary>
    /// Verifies that two rows with the same title and date, but different times, are not
    /// considered duplicates of each other (the duplicate key compares the exact date and time).
    /// </summary>
    [Fact]
    public async Task ValidateAsync_WhenTitleAndDateMatchButTimeDiffers_DoesNotMarkAsDuplicate()
    {
        // Arrange
        var context = TestDbContextFactory.CreateTestContext();
        var service = CreateService(context);
        var candidates = new List<ImportEventItemDto>
        {
            CreateItem(title: "Trap Shooting", date: new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc)),
            CreateItem(title: "Trap Shooting", date: new DateTime(2026, 6, 1, 16, 0, 0, DateTimeKind.Utc))
        };

        // Act
        var results = await service.ValidateAsync(candidates, CancellationToken.None);

        // Assert
        results[0].IsDuplicate.Should().BeFalse();
        results[1].IsDuplicate.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that a candidate whose (title, date) key matches an already-persisted event is
    /// flagged as a duplicate, with the expected error message.
    /// </summary>
    [Fact]
    public async Task ValidateAsync_WhenRowMatchesPersistedEvent_MarksAsDuplicate()
    {
        // Arrange
        var sharedDate = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);
        var existingEvent = new Event
        {
            Title = "Trap Shooting",
            Date = sharedDate,
            Location = "Range 1",
            MaxCapacity = 30
        };
        var context = TestDbContextFactory.CreateTestContextWithEvents([existingEvent]);
        var service = CreateService(context);
        var candidate = CreateItem(title: "TRAP SHOOTING", date: sharedDate);

        // Act
        var results = await service.ValidateAsync([candidate], CancellationToken.None);

        // Assert
        results.Single().IsDuplicate.Should().BeTrue();
        results.Single().IsValid.Should().BeFalse();
        results.Single().Errors.Should().Contain("An event with this title and date already exists");
    }

    /// <summary>
    /// Verifies that a batch with no matching keys, either within itself or against persisted
    /// events, leaves every row valid and not flagged as a duplicate.
    /// </summary>
    [Fact]
    public async Task ValidateAsync_WhenNoDuplicatesExist_AllRowsAreValidAndNotDuplicate()
    {
        // Arrange
        var context = TestDbContextFactory.CreateTestContext();
        var service = CreateService(context);
        var candidates = new List<ImportEventItemDto>
        {
            CreateItem(title: "Trap Shooting", date: new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc)),
            CreateItem(title: "Skeet Shooting", date: new DateTime(2026, 6, 2, 10, 0, 0, DateTimeKind.Utc))
        };

        // Act
        var results = await service.ValidateAsync(candidates, CancellationToken.None);

        // Assert
        results.Should().OnlyContain(r => r.IsValid && !r.IsDuplicate);
    }

    /// <summary>
    /// Verifies that a persisted event sitting at the far edge of the batch's date range (the
    /// last moment of the last day, rather than midnight) is still picked up by the bounded
    /// duplicate-detection query, guarding against an off-by-one error in the upper bound.
    /// </summary>
    [Fact]
    public async Task ValidateAsync_WhenPersistedEventIsAtUpperRangeBoundary_DetectsDuplicate()
    {
        // Arrange
        var lateNightDate = new DateTime(2026, 6, 10, 23, 59, 0, DateTimeKind.Utc);
        var existingEvent = new Event
        {
            Title = "Late Night Session",
            Date = lateNightDate,
            Location = "Range 1",
            MaxCapacity = 30
        };
        var context = TestDbContextFactory.CreateTestContextWithEvents([existingEvent]);
        var service = CreateService(context);
        var candidate = CreateItem(title: "Late Night Session", date: lateNightDate);

        // Act
        var results = await service.ValidateAsync([candidate], CancellationToken.None);

        // Assert
        results.Single().IsDuplicate.Should().BeTrue();
        results.Single().Errors.Should().Contain("An event with this title and date already exists");
    }

    /// <summary>
    /// Verifies that a candidate failing field-level validation (delegated to the shared
    /// <see cref="ImportEventItemDtoValidator"/>) is marked invalid with the field error surfaced,
    /// while <c>IsDuplicate</c> stays false, confirming field validation and duplicate detection
    /// are independent error sources that both feed into the combined <c>Errors</c>/<c>IsValid</c> result.
    /// </summary>
    [Fact]
    public async Task ValidateAsync_WhenCandidateFailsFieldValidation_MarksInvalidWithFieldErrorAndNotDuplicate()
    {
        // Arrange
        var context = TestDbContextFactory.CreateTestContext();
        var service = CreateService(context);
        var candidate = CreateItem(title: string.Empty);

        // Act
        var results = await service.ValidateAsync([candidate], CancellationToken.None);

        // Assert
        results.Single().IsValid.Should().BeFalse();
        results.Single().IsDuplicate.Should().BeFalse();
        results.Single().Errors.Should().NotBeEmpty();
    }

    /// <summary>
    /// Verifies that a row can accumulate both a field-level error and a duplicate error at the
    /// same time (e.g. an intra-batch duplicate whose title is also blank), and that both error
    /// messages are present in the combined <c>Errors</c> list rather than one masking the other.
    /// </summary>
    [Fact]
    public async Task ValidateAsync_WhenRowIsBothFieldInvalidAndDuplicate_CombinesBothErrorSources()
    {
        // Arrange
        var context = TestDbContextFactory.CreateTestContext();
        var service = CreateService(context);
        var sharedDate = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);
        var candidates = new List<ImportEventItemDto>
        {
            CreateItem(title: string.Empty, date: sharedDate),
            CreateItem(title: string.Empty, date: sharedDate)
        };

        // Act
        var results = await service.ValidateAsync(candidates, CancellationToken.None);

        // Assert
        results[1].IsValid.Should().BeFalse();
        results[1].IsDuplicate.Should().BeTrue();
        results[1].Errors.Should().Contain("Duplicate of row 1");
        results[1].Errors.Should().Contain(e => e != "Duplicate of row 1");
    }
}
