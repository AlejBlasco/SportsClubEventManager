using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SportsClubEventManager.Infrastructure.Import;
using Xunit;

namespace SportsClubEventManager.Tests.Infrastructure.Import;

/// <summary>
/// Tests for CsvEventImportParser covering column mapping, date/time combination, composite
/// description building, encoding handling, and structural error detection.
/// </summary>
public sealed class CsvEventImportParserTests
{
    private const string StandardHeader = "DÍA,MODAL.,NOMBRE TIRADA,HORA,CAMPO,LUGAR,CAT";

    private static CsvEventImportParser CreateParser(Dictionary<string, string?>? settings = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings ?? [])
            .Build();

        var logger = Substitute.For<ILogger<CsvEventImportParser>>();
        return new CsvEventImportParser(configuration, logger);
    }

    private static Stream ToStream(string content, bool withBom = false)
    {
        var encoding = new UTF8Encoding(withBom);
        return new MemoryStream(encoding.GetBytes(content));
    }

    /// <summary>
    /// Verifies that a valid, flat single-header-row CSV file is parsed and mapped correctly.
    /// </summary>
    [Fact]
    public void Parse_WhenFileIsValidFlatCsv_ReturnsMappedRow()
    {
        // Arrange
        var parser = CreateParser();
        var csv = StandardHeader + "\r\n" +
            "15/09/2026,Trap,1ª Tirada El Balín,10:00,Campo 2,Club de Tiro Norte,S1\r\n";

        // Act
        var result = parser.Parse(ToStream(csv), null, defaultMaxCapacity: 30, CancellationToken.None);

        // Assert
        result.FatalError.Should().BeNull();
        var row = result.Rows.Single();
        row.Title.Should().Be("1ª Tirada El Balín");
        row.Location.Should().Be("Club de Tiro Norte");
        row.Date.Should().Be(new DateTime(2026, 9, 15, 10, 0, 0));
        row.Description.Should().Be("Modality: Trap | Field: Campo 2 | Category: S1");
        row.MaxCapacity.Should().Be(30);
        row.SourceDay.Should().Be("15/09/2026");
        row.SourceTime.Should().Be("10:00");
        row.Errors.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that an unparseable "DÍA" value produces a row-level error and a null date.
    /// </summary>
    [Fact]
    public void Parse_WhenDiaIsMalformed_ReturnsRowLevelError()
    {
        // Arrange
        var parser = CreateParser();
        var csv = StandardHeader + "\r\n" +
            "not-a-date,Trap,Event,10:00,Campo 2,Club de Tiro Norte,S1\r\n";

        // Act
        var result = parser.Parse(ToStream(csv), null, defaultMaxCapacity: 30, CancellationToken.None);

        // Assert
        var row = result.Rows.Single();
        row.Date.Should().BeNull();
        row.Errors.Should().ContainSingle(e => e.Contains("DÍA"));
    }

    /// <summary>
    /// Verifies that a blank "HORA" value falls back to the configured default event time.
    /// </summary>
    [Fact]
    public void Parse_WhenHoraIsBlank_UsesConfiguredDefaultEventTime()
    {
        // Arrange
        var parser = CreateParser(new Dictionary<string, string?> { { "ImportSettings:DefaultEventTime", "14:30" } });
        var csv = StandardHeader + "\r\n" +
            "15/09/2026,Trap,Event,,Campo 2,Club de Tiro Norte,S1\r\n";

        // Act
        var result = parser.Parse(ToStream(csv), null, defaultMaxCapacity: 30, CancellationToken.None);

        // Assert
        var row = result.Rows.Single();
        row.Date.Should().Be(new DateTime(2026, 9, 15, 14, 30, 0));
        row.Errors.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that blank "MODAL."/"CAMPO"/"CAT" segments are omitted from the composite description.
    /// </summary>
    [Fact]
    public void Parse_WhenModalityFieldAndCategoryAreBlank_OmitsBlankSegmentsFromDescription()
    {
        // Arrange
        var parser = CreateParser();
        var csv = StandardHeader + "\r\n" +
            "15/09/2026,,Event,10:00,,Club de Tiro Norte,S1\r\n";

        // Act
        var result = parser.Parse(ToStream(csv), null, defaultMaxCapacity: 30, CancellationToken.None);

        // Assert
        var row = result.Rows.Single();
        row.Description.Should().Be("Category: S1");
    }

    /// <summary>
    /// Verifies that a completely blank modality/field/category set produces a null description.
    /// </summary>
    [Fact]
    public void Parse_WhenModalityFieldAndCategoryAreAllBlank_ReturnsNullDescription()
    {
        // Arrange
        var parser = CreateParser();
        var csv = StandardHeader + "\r\n" +
            "15/09/2026,,Event,10:00,,Club de Tiro Norte,\r\n";

        // Act
        var result = parser.Parse(ToStream(csv), null, defaultMaxCapacity: 30, CancellationToken.None);

        // Assert
        result.Rows.Single().Description.Should().BeNull();
    }

    /// <summary>
    /// Verifies that a missing required column among the standardized 7 produces a structural fatal error.
    /// </summary>
    [Fact]
    public void Parse_WhenRequiredHeaderIsMissing_ReturnsFatalError()
    {
        // Arrange
        var parser = CreateParser();
        var csv = "DÍA,MODAL.,NOMBRE TIRADA,HORA,CAMPO,LUGAR\r\n" +
            "15/09/2026,Trap,Event,10:00,Campo 2,Club de Tiro Norte\r\n";

        // Act
        var result = parser.Parse(ToStream(csv), null, defaultMaxCapacity: 30, CancellationToken.None);

        // Assert
        result.FatalError.Should().NotBeNull();
        result.FatalError.Should().Contain("CAT");
        result.Rows.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a UTF-8 BOM-prefixed file is still read correctly, preserving the accented "DÍA" header.
    /// </summary>
    [Fact]
    public void Parse_WhenFileHasUtf8Bom_ReadsAccentedHeaderCorrectly()
    {
        // Arrange
        var parser = CreateParser();
        var csv = StandardHeader + "\r\n" +
            "15/09/2026,Trap,Event,10:00,Campo 2,Club de Tiro Norte,S1\r\n";

        // Act
        var result = parser.Parse(ToStream(csv, withBom: true), null, defaultMaxCapacity: 30, CancellationToken.None);

        // Assert
        result.FatalError.Should().BeNull();
        result.DetectedHeaders.Should().Contain("DÍA");
        result.Rows.Single().Errors.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that an explicit column mapping resolves a renamed source header to the standardized column.
    /// </summary>
    [Fact]
    public void Parse_WhenColumnMappingProvided_MapsRenamedHeaderToCanonicalColumn()
    {
        // Arrange
        var parser = CreateParser();
        var csv = "Day,MODAL.,NOMBRE TIRADA,HORA,CAMPO,LUGAR,CAT\r\n" +
            "15/09/2026,Trap,Event,10:00,Campo 2,Club de Tiro Norte,S1\r\n";
        var mapping = new Dictionary<string, string> { { "Day", "DÍA" } };

        // Act
        var result = parser.Parse(ToStream(csv), mapping, defaultMaxCapacity: 30, CancellationToken.None);

        // Assert
        result.FatalError.Should().BeNull();
        result.Rows.Single().Date.Should().Be(new DateTime(2026, 9, 15, 10, 0, 0));
    }

    /// <summary>
    /// Verifies that a file exceeding the configured maximum row count is rejected outright with a fatal error.
    /// </summary>
    [Fact]
    public void Parse_WhenRowCountExceedsConfiguredMax_ReturnsFatalError()
    {
        // Arrange
        var parser = CreateParser(new Dictionary<string, string?> { { "ImportSettings:MaxRowCount", "1" } });
        var csv = StandardHeader + "\r\n" +
            "15/09/2026,Trap,Event 1,10:00,Campo 2,Club de Tiro Norte,S1\r\n" +
            "16/09/2026,Trap,Event 2,10:00,Campo 2,Club de Tiro Norte,S1\r\n";

        // Act
        var result = parser.Parse(ToStream(csv), null, defaultMaxCapacity: 30, CancellationToken.None);

        // Assert
        result.FatalError.Should().NotBeNull();
        result.Rows.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that an empty file produces a structural fatal error rather than an unhandled exception.
    /// </summary>
    [Fact]
    public void Parse_WhenFileIsEmpty_ReturnsFatalError()
    {
        // Arrange
        var parser = CreateParser();

        // Act
        var result = parser.Parse(ToStream(string.Empty), null, defaultMaxCapacity: 30, CancellationToken.None);

        // Assert
        result.FatalError.Should().NotBeNull();
    }
}
