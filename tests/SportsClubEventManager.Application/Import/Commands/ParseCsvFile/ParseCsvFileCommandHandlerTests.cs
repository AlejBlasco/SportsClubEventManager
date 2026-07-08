using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Application.Common.Validators;
using SportsClubEventManager.Application.Import.Commands.ParseCsvFile;
using SportsClubEventManager.Application.Import.Models;
using SportsClubEventManager.Shared.DTOs;
using Xunit;

namespace SportsClubEventManager.Application.Tests.Import.Commands.ParseCsvFile;

/// <summary>
/// Tests for ParseCsvFileCommandHandler to verify preview mapping, field-level validation, and fatal-error handling.
/// </summary>
public sealed class ParseCsvFileCommandHandlerTests
{
    private readonly ICsvEventImportParser _parser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ParseCsvFileCommandHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="ParseCsvFileCommandHandlerTests"/> class.
    /// </summary>
    public ParseCsvFileCommandHandlerTests()
    {
        _parser = Substitute.For<ICsvEventImportParser>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _dateTimeProvider.UtcNow.Returns(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ImportSettings:DefaultMaxCapacity", "30" }
            })
            .Build();

        var itemValidator = new ImportEventItemDtoValidator(_dateTimeProvider);
        var logger = Substitute.For<ILogger<ParseCsvFileCommandHandler>>();

        _handler = new ParseCsvFileCommandHandler(_parser, configuration, itemValidator, logger);
    }

    private static ParseCsvFileCommand CreateCommand(int? defaultMaxCapacity = null) => new()
    {
        FileStream = new MemoryStream(),
        FileName = "events.csv",
        DefaultMaxCapacity = defaultMaxCapacity
    };

    /// <summary>
    /// Verifies that a fully valid parsed row is mapped to a valid preview row with no errors.
    /// </summary>
    [Fact]
    public async Task Handle_WhenRowIsFullyValid_ReturnsValidPreviewRow()
    {
        // Arrange
        var futureDate = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);
        _parser.Parse(Arg.Any<Stream>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new CsvParseResult
            {
                DetectedHeaders = ["DÍA", "MODAL.", "NOMBRE TIRADA", "HORA", "CAMPO", "LUGAR", "CAT"],
                Rows =
                [
                    new ImportRowParseResult
                    {
                        RowNumber = 1,
                        Title = "1ª Tirada El Balín",
                        Date = futureDate,
                        Description = "Modality: Trap | Field: Campo 2 | Category: S1",
                        Location = "Club de Tiro Norte",
                        MaxCapacity = 30,
                        SourceDay = "01/06/2026",
                        SourceTime = "10:00",
                        SourceModality = "Trap",
                        SourceField = "Campo 2",
                        SourceCategory = "S1",
                        Errors = []
                    }
                ]
            });

        // Act
        var result = await _handler.Handle(CreateCommand(), CancellationToken.None);

        // Assert
        result.FatalError.Should().BeNull();
        result.TotalRows.Should().Be(1);
        result.ValidRowCount.Should().Be(1);
        result.InvalidRowCount.Should().Be(0);
        result.Rows.Single().IsValid.Should().BeTrue();
        result.Rows.Single().Errors.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a row missing its title is marked invalid with a corresponding error message.
    /// </summary>
    [Fact]
    public async Task Handle_WhenRowIsMissingTitle_ReturnsInvalidRowWithError()
    {
        // Arrange
        _parser.Parse(Arg.Any<Stream>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new CsvParseResult
            {
                Rows =
                [
                    new ImportRowParseResult
                    {
                        RowNumber = 2,
                        Title = null,
                        Date = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc),
                        Location = "Club de Tiro Norte",
                        MaxCapacity = 30,
                        Errors = []
                    }
                ]
            });

        // Act
        var result = await _handler.Handle(CreateCommand(), CancellationToken.None);

        // Assert
        var row = result.Rows.Single();
        row.IsValid.Should().BeFalse();
        row.Errors.Should().Contain(e => e.Contains("title", StringComparison.OrdinalIgnoreCase));
        result.InvalidRowCount.Should().Be(1);
    }

    /// <summary>
    /// Verifies that a parser-reported parsing error (e.g. an unparseable date) is preserved
    /// alongside any additional field-level validation errors.
    /// </summary>
    [Fact]
    public async Task Handle_WhenRowHasParsingError_PreservesParserErrorInResult()
    {
        // Arrange
        _parser.Parse(Arg.Any<Stream>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new CsvParseResult
            {
                Rows =
                [
                    new ImportRowParseResult
                    {
                        RowNumber = 3,
                        Title = "Valid Title",
                        Date = null,
                        Location = "Valid Location",
                        MaxCapacity = 30,
                        Errors = ["Row 3: unable to parse \"DÍA\" value 'not-a-date'. Expected format dd/MM/yyyy or yyyy-MM-dd."]
                    }
                ]
            });

        // Act
        var result = await _handler.Handle(CreateCommand(), CancellationToken.None);

        // Assert
        var row = result.Rows.Single();
        row.IsValid.Should().BeFalse();
        row.Errors.Should().Contain(e => e.Contains("DÍA"));
    }

    /// <summary>
    /// Verifies that a structural (fatal) parser error short-circuits the response with no rows.
    /// </summary>
    [Fact]
    public async Task Handle_WhenParserReturnsFatalError_ReturnsFatalErrorWithoutRows()
    {
        // Arrange
        _parser.Parse(Arg.Any<Stream>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new CsvParseResult
            {
                DetectedHeaders = ["Title", "Date"],
                FatalError = "Missing required column(s): DÍA, MODAL., NOMBRE TIRADA, HORA, CAMPO, LUGAR, CAT."
            });

        // Act
        var result = await _handler.Handle(CreateCommand(), CancellationToken.None);

        // Assert
        result.FatalError.Should().NotBeNull();
        result.Rows.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that the configured default maximum capacity is passed to the parser when the
    /// command does not supply an explicit override.
    /// </summary>
    [Fact]
    public async Task Handle_WhenDefaultMaxCapacityNotSupplied_UsesConfiguredFallback()
    {
        // Arrange
        _parser.Parse(Arg.Any<Stream>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new CsvParseResult());

        // Act
        await _handler.Handle(CreateCommand(), CancellationToken.None);

        // Assert
        _parser.Received(1).Parse(Arg.Any<Stream>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), 30, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that an explicit default maximum capacity override in the command takes priority
    /// over the configured fallback.
    /// </summary>
    [Fact]
    public async Task Handle_WhenDefaultMaxCapacitySupplied_UsesRequestOverride()
    {
        // Arrange
        _parser.Parse(Arg.Any<Stream>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new CsvParseResult());

        // Act
        await _handler.Handle(CreateCommand(defaultMaxCapacity: 75), CancellationToken.None);

        // Assert
        _parser.Received(1).Parse(Arg.Any<Stream>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), 75, Arg.Any<CancellationToken>());
    }
}
