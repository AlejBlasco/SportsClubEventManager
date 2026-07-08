using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SportsClubEventManager.Application.Common.Constants;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Application.Import.Models;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Application.Import.Commands.ParseCsvFile;

/// <summary>
/// Handler for parsing an uploaded CSV file into a preview of mapped event rows.
/// Delegates the actual CSV parsing/mapping to <see cref="ICsvEventImportParser"/>, then
/// delegates normalization, field-level validation and duplicate detection for the whole batch,
/// in a single call, to <see cref="IEventImportValidationService"/>. Performs no database writes
/// and is not audited, since nothing changed yet.
/// </summary>
public sealed class ParseCsvFileCommandHandler : IRequestHandler<ParseCsvFileCommand, CsvImportPreviewResponse>
{
    private readonly ICsvEventImportParser _parser;
    private readonly IConfiguration _configuration;
    private readonly IEventImportValidationService _validationService;
    private readonly ILogger<ParseCsvFileCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ParseCsvFileCommandHandler"/> class.
    /// </summary>
    /// <param name="parser">The CSV parser used to read and map the uploaded file.</param>
    /// <param name="configuration">The application configuration, used to resolve import defaults.</param>
    /// <param name="validationService">The batch normalization/validation/duplicate-detection service.</param>
    /// <param name="logger">The logger for structured, content-free diagnostics.</param>
    public ParseCsvFileCommandHandler(
        ICsvEventImportParser parser,
        IConfiguration configuration,
        IEventImportValidationService validationService,
        ILogger<ParseCsvFileCommandHandler> logger)
    {
        _parser = parser;
        _configuration = configuration;
        _validationService = validationService;
        _logger = logger;
    }

    /// <summary>
    /// Handles the command by parsing the uploaded file and validating the whole batch of mapped rows.
    /// </summary>
    /// <param name="request">The command containing the uploaded file and optional overrides.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The preview response, including per-row validation results.</returns>
    public async Task<CsvImportPreviewResponse> Handle(ParseCsvFileCommand request, CancellationToken cancellationToken)
    {
        var defaultMaxCapacity = request.DefaultMaxCapacity
            ?? _configuration.GetValue(ImportSettingsKeys.DefaultMaxCapacity, ImportSettingsKeys.DefaultMaxCapacityFallback);

        _logger.LogInformation(
            "Parsing CSV import file {FileName} with default max capacity {DefaultMaxCapacity}.",
            request.FileName,
            defaultMaxCapacity);

        var parseResult = _parser.Parse(request.FileStream, request.ColumnMapping, defaultMaxCapacity, cancellationToken);

        if (parseResult.FatalError is not null)
        {
            _logger.LogWarning(
                "CSV import file {FileName} could not be parsed: {FatalError}",
                request.FileName,
                parseResult.FatalError);

            return new CsvImportPreviewResponse
            {
                DetectedHeaders = parseResult.DetectedHeaders,
                SuggestedMapping = parseResult.SuggestedMapping,
                FatalError = parseResult.FatalError
            };
        }

        var candidates = parseResult.Rows.Select(ToCandidate).ToList();
        var validationResults = await _validationService.ValidateAsync(candidates, cancellationToken);
        var rows = parseResult.Rows.Zip(validationResults, BuildRowDto).ToList();
        var validRowCount = rows.Count(r => r.IsValid);

        _logger.LogInformation(
            "Parsed CSV import file {FileName}: {TotalRows} rows, {ValidRowCount} valid, {InvalidRowCount} invalid.",
            request.FileName,
            rows.Count,
            validRowCount,
            rows.Count - validRowCount);

        return new CsvImportPreviewResponse
        {
            DetectedHeaders = parseResult.DetectedHeaders,
            SuggestedMapping = parseResult.SuggestedMapping,
            Rows = rows,
            TotalRows = rows.Count,
            ValidRowCount = validRowCount,
            InvalidRowCount = rows.Count - validRowCount
        };
    }

    /// <summary>
    /// Maps a raw parsed row to the candidate shape expected by <see cref="IEventImportValidationService"/>.
    /// </summary>
    /// <param name="row">The parsed row to convert.</param>
    /// <returns>The candidate, with any unparseable fields defaulted (already reflected in <see cref="ImportRowParseResult.Errors"/>).</returns>
    private static ImportEventItemDto ToCandidate(ImportRowParseResult row) => new()
    {
        Title = row.Title ?? string.Empty,
        Date = row.Date ?? default,
        Description = row.Description,
        Location = row.Location ?? string.Empty,
        MaxCapacity = row.MaxCapacity ?? 0
    };

    /// <summary>
    /// Combines a parsed row's metadata (row number, raw source columns, parsing errors) with its
    /// batch validation result into the preview DTO.
    /// </summary>
    /// <param name="row">The parsed row, providing row metadata and any parsing errors.</param>
    /// <param name="validationResult">The corresponding batch validation result for this row.</param>
    /// <returns>The preview row DTO, including its combined error list.</returns>
    private static CsvImportRowDto BuildRowDto(ImportRowParseResult row, ImportRowValidationResult validationResult)
    {
        var errors = row.Errors
            .Concat(validationResult.Errors)
            .Distinct()
            .ToList();

        return new CsvImportRowDto
        {
            RowNumber = row.RowNumber,
            Title = validationResult.NormalizedItem.Title,
            Date = row.Date,
            Description = validationResult.NormalizedItem.Description,
            Location = validationResult.NormalizedItem.Location,
            MaxCapacity = row.MaxCapacity,
            SourceDay = row.SourceDay,
            SourceTime = row.SourceTime,
            SourceModality = row.SourceModality,
            SourceField = row.SourceField,
            SourceCategory = row.SourceCategory,
            IsValid = errors.Count == 0,
            IsDuplicate = validationResult.IsDuplicate,
            Errors = errors
        };
    }
}
