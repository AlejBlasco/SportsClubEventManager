using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SportsClubEventManager.Application.Common.Constants;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Application.Import.Models;

namespace SportsClubEventManager.Infrastructure.Import;

/// <summary>
/// <see cref="ICsvEventImportParser"/> implementation using CsvHelper, mapping the standardized
/// 7-column header ("DÍA,MODAL.,NOMBRE TIRADA,HORA,CAMPO,LUGAR,CAT") to the writable
/// <c>Event</c> fields.
/// </summary>
public sealed class CsvEventImportParser : ICsvEventImportParser
{
    private static readonly string[] CanonicalColumns =
    [
        "DÍA", "MODAL.", "NOMBRE TIRADA", "HORA", "CAMPO", "LUGAR", "CAT"
    ];

    private static readonly string[] DateFormats = ["dd/MM/yyyy", "yyyy-MM-dd"];

    private readonly IConfiguration _configuration;
    private readonly ILogger<CsvEventImportParser> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CsvEventImportParser"/> class.
    /// </summary>
    /// <param name="configuration">The application configuration, used to resolve import defaults.</param>
    /// <param name="logger">The logger for structured, content-free diagnostics.</param>
    public CsvEventImportParser(IConfiguration configuration, ILogger<CsvEventImportParser> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public CsvParseResult Parse(
        Stream csvStream,
        IReadOnlyDictionary<string, string>? columnMapping,
        int defaultMaxCapacity,
        CancellationToken cancellationToken)
    {
        var maxRowCount = _configuration.GetValue(ImportSettingsKeys.MaxRowCount, ImportSettingsKeys.MaxRowCountFallback);
        var defaultEventTimeRaw = _configuration.GetValue(ImportSettingsKeys.DefaultEventTime, ImportSettingsKeys.DefaultEventTimeFallback)
            ?? ImportSettingsKeys.DefaultEventTimeFallback;

        try
        {
            return ParseCore(csvStream, columnMapping, defaultMaxCapacity, defaultEventTimeRaw, maxRowCount, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Any unexpected parsing failure (unreadable stream, unsupported encoding, malformed
            // CSV structure) degrades to a structural FatalError rather than a 500, per the
            // two-tier error-handling model: never log the exception's row-content details.
            _logger.LogError(ex, "Unable to parse the uploaded CSV import file.");
            return new CsvParseResult
            {
                FatalError = "Unable to read the uploaded file. Ensure it is a valid, UTF-8 encoded CSV file using the standardized header."
            };
        }
    }

    private CsvParseResult ParseCore(
        Stream csvStream,
        IReadOnlyDictionary<string, string>? columnMapping,
        int defaultMaxCapacity,
        string defaultEventTimeRaw,
        int maxRowCount,
        CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(
            csvStream,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            detectEncodingFromByteOrderMarks: true,
            leaveOpen: true);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
            TrimOptions = TrimOptions.Trim
        };

        using var csv = new CsvReader(reader, config);

        if (!csv.Read())
        {
            return new CsvParseResult
            {
                FatalError = "The uploaded file is empty."
            };
        }

        csv.ReadHeader();

        if (csv.HeaderRecord is null || csv.HeaderRecord.Length == 0)
        {
            return new CsvParseResult
            {
                FatalError = "The uploaded file does not contain a valid header row."
            };
        }

        var detectedHeaders = csv.HeaderRecord.ToList();
        var suggestedMapping = BuildSuggestedMapping(detectedHeaders);
        var (columnLookup, missingColumns) = ResolveColumns(detectedHeaders, columnMapping);

        if (missingColumns.Count > 0)
        {
            _logger.LogWarning(
                "CSV import file is missing required column(s): {MissingColumns}",
                string.Join(", ", missingColumns));

            return new CsvParseResult
            {
                DetectedHeaders = detectedHeaders,
                SuggestedMapping = suggestedMapping,
                FatalError = $"Missing required column(s): {string.Join(", ", missingColumns)}. " +
                    $"Expected header: {string.Join(",", CanonicalColumns)}."
            };
        }

        var rows = new List<ImportRowParseResult>();
        var rowNumber = 0;

        while (csv.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();
            rowNumber++;

            if (rowNumber > maxRowCount)
            {
                _logger.LogWarning(
                    "CSV import file exceeds the maximum allowed row count of {MaxRowCount}.",
                    maxRowCount);

                return new CsvParseResult
                {
                    DetectedHeaders = detectedHeaders,
                    SuggestedMapping = suggestedMapping,
                    FatalError = $"The file exceeds the maximum allowed row count of {maxRowCount}."
                };
            }

            rows.Add(BuildRow(csv, rowNumber, columnLookup, defaultMaxCapacity, defaultEventTimeRaw));
        }

        return new CsvParseResult
        {
            DetectedHeaders = detectedHeaders,
            SuggestedMapping = suggestedMapping,
            Rows = rows
        };
    }

    /// <summary>
    /// Builds one mapped row, combining "DÍA"/"HORA" into a single date/time and composing
    /// the "MODAL."/"CAMPO"/"CAT" columns into the description.
    /// </summary>
    private static ImportRowParseResult BuildRow(
        CsvReader csv,
        int rowNumber,
        IReadOnlyDictionary<string, string> columnLookup,
        int defaultMaxCapacity,
        string defaultEventTimeRaw)
    {
        var errors = new List<string>();

        var rawDay = GetField(csv, columnLookup, "DÍA");
        var rawTime = GetField(csv, columnLookup, "HORA");
        var rawModality = GetField(csv, columnLookup, "MODAL.");
        var rawTitle = GetField(csv, columnLookup, "NOMBRE TIRADA");
        var rawField = GetField(csv, columnLookup, "CAMPO");
        var rawLocation = GetField(csv, columnLookup, "LUGAR");
        var rawCategory = GetField(csv, columnLookup, "CAT");

        var date = ParseDay(rawDay, rowNumber, errors);
        var time = ParseTime(rawTime, defaultEventTimeRaw, rowNumber, errors);

        DateTime? combinedDate = date.HasValue && time.HasValue
            ? date.Value.Date + time.Value
            : null;

        return new ImportRowParseResult
        {
            RowNumber = rowNumber,
            Title = NullIfBlank(rawTitle),
            Date = combinedDate,
            Description = EventDescriptionComposer.Compose(rawModality, rawField, rawCategory),
            Location = NullIfBlank(rawLocation),
            MaxCapacity = defaultMaxCapacity,
            SourceDay = rawDay,
            SourceTime = rawTime,
            SourceModality = rawModality,
            SourceField = rawField,
            SourceCategory = rawCategory,
            Errors = errors
        };
    }

    /// <summary>
    /// Parses the "DÍA" column using the dd/MM/yyyy (primary) / yyyy-MM-dd (fallback) format allow-list.
    /// </summary>
    private static DateTime? ParseDay(string? rawDay, int rowNumber, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(rawDay))
        {
            errors.Add($"Row {rowNumber}: \"DÍA\" is required.");
            return null;
        }

        if (DateTime.TryParseExact(rawDay.Trim(), DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
        {
            return parsedDate;
        }

        errors.Add($"Row {rowNumber}: unable to parse \"DÍA\" value '{rawDay}'. Expected format dd/MM/yyyy or yyyy-MM-dd.");
        return null;
    }

    /// <summary>
    /// Parses the "HORA" column (24-hour HH:mm), falling back to the configured default event
    /// time when blank.
    /// </summary>
    private static TimeSpan? ParseTime(string? rawTime, string defaultEventTimeRaw, int rowNumber, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(rawTime))
        {
            if (DateTime.TryParseExact(defaultEventTimeRaw, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var defaultTime))
            {
                return defaultTime.TimeOfDay;
            }

            errors.Add($"Row {rowNumber}: the configured default event time '{defaultEventTimeRaw}' is invalid.");
            return null;
        }

        if (DateTime.TryParseExact(rawTime.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedTime))
        {
            return parsedTime.TimeOfDay;
        }

        errors.Add($"Row {rowNumber}: unable to parse \"HORA\" value '{rawTime}'. Expected format HH:mm.");
        return null;
    }

    /// <summary>
    /// Reads a field by its canonical column name, resolving through the source-to-canonical lookup.
    /// </summary>
    private static string? GetField(CsvReader csv, IReadOnlyDictionary<string, string> columnLookup, string canonicalName)
    {
        if (!columnLookup.TryGetValue(canonicalName, out var actualHeader))
        {
            return null;
        }

        return csv.TryGetField<string>(actualHeader, out var value) ? value : null;
    }

    /// <summary>
    /// Resolves the actual source header for each of the 7 canonical columns, honoring an
    /// explicit column mapping when supplied and falling back to a case-insensitive exact match.
    /// </summary>
    private static (Dictionary<string, string> ColumnLookup, List<string> MissingColumns) ResolveColumns(
        IReadOnlyList<string> detectedHeaders,
        IReadOnlyDictionary<string, string>? columnMapping)
    {
        var columnLookup = new Dictionary<string, string>();
        var missingColumns = new List<string>();

        foreach (var canonical in CanonicalColumns)
        {
            var actualHeader = ResolveActualHeader(canonical, detectedHeaders, columnMapping);

            if (actualHeader is null)
            {
                missingColumns.Add(canonical);
            }
            else
            {
                columnLookup[canonical] = actualHeader;
            }
        }

        return (columnLookup, missingColumns);
    }

    /// <summary>
    /// Resolves the source header mapped to a single canonical column name.
    /// </summary>
    private static string? ResolveActualHeader(
        string canonical,
        IReadOnlyList<string> detectedHeaders,
        IReadOnlyDictionary<string, string>? columnMapping)
    {
        if (columnMapping is not null)
        {
            foreach (var (sourceHeader, targetColumn) in columnMapping)
            {
                if (!string.Equals(targetColumn, canonical, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var explicitMatch = detectedHeaders.FirstOrDefault(h => string.Equals(h, sourceHeader, StringComparison.OrdinalIgnoreCase));

                if (explicitMatch is not null)
                {
                    return explicitMatch;
                }
            }
        }

        return detectedHeaders.FirstOrDefault(h => string.Equals(h.Trim(), canonical, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Builds a best-effort suggested mapping from detected headers to the standardized column
    /// names, based on a case-insensitive exact match, for pre-filling the remap UI.
    /// </summary>
    private static Dictionary<string, string> BuildSuggestedMapping(IReadOnlyList<string> detectedHeaders)
    {
        var mapping = new Dictionary<string, string>();

        foreach (var header in detectedHeaders)
        {
            var match = CanonicalColumns.FirstOrDefault(c => string.Equals(c, header.Trim(), StringComparison.OrdinalIgnoreCase));

            if (match is not null)
            {
                mapping[header] = match;
            }
        }

        return mapping;
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
