using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SportsClubEventManager.Application.Common.Constants;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Application.Common.Validators;
using SportsClubEventManager.Application.Import.Models;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Application.Import.Commands.ParseCsvFile;

/// <summary>
/// Handler for parsing an uploaded CSV file into a preview of mapped event rows.
/// Delegates the actual CSV parsing/mapping to <see cref="ICsvEventImportParser"/>, then
/// applies field-level validation (mirroring <c>CreateEventCommandValidator</c>'s rules) to
/// every row. Performs no database writes and is not audited, since nothing changed yet.
/// </summary>
public sealed class ParseCsvFileCommandHandler : IRequestHandler<ParseCsvFileCommand, CsvImportPreviewResponse>
{
    private readonly ICsvEventImportParser _parser;
    private readonly IConfiguration _configuration;
    private readonly IValidator<ImportEventItemDto> _itemValidator;
    private readonly ILogger<ParseCsvFileCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ParseCsvFileCommandHandler"/> class.
    /// </summary>
    /// <param name="parser">The CSV parser used to read and map the uploaded file.</param>
    /// <param name="configuration">The application configuration, used to resolve import defaults.</param>
    /// <param name="itemValidator">The shared field-level validator for mapped event rows.</param>
    /// <param name="logger">The logger for structured, content-free diagnostics.</param>
    public ParseCsvFileCommandHandler(
        ICsvEventImportParser parser,
        IConfiguration configuration,
        IValidator<ImportEventItemDto> itemValidator,
        ILogger<ParseCsvFileCommandHandler> logger)
    {
        _parser = parser;
        _configuration = configuration;
        _itemValidator = itemValidator;
        _logger = logger;
    }

    /// <summary>
    /// Handles the command by parsing the uploaded file and validating every mapped row.
    /// </summary>
    /// <param name="request">The command containing the uploaded file and optional overrides.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The preview response, including per-row validation results.</returns>
    public Task<CsvImportPreviewResponse> Handle(ParseCsvFileCommand request, CancellationToken cancellationToken)
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

            return Task.FromResult(new CsvImportPreviewResponse
            {
                DetectedHeaders = parseResult.DetectedHeaders,
                SuggestedMapping = parseResult.SuggestedMapping,
                FatalError = parseResult.FatalError
            });
        }

        var rows = parseResult.Rows.Select(BuildRowDto).ToList();
        var validRowCount = rows.Count(r => r.IsValid);

        _logger.LogInformation(
            "Parsed CSV import file {FileName}: {TotalRows} rows, {ValidRowCount} valid, {InvalidRowCount} invalid.",
            request.FileName,
            rows.Count,
            validRowCount,
            rows.Count - validRowCount);

        return Task.FromResult(new CsvImportPreviewResponse
        {
            DetectedHeaders = parseResult.DetectedHeaders,
            SuggestedMapping = parseResult.SuggestedMapping,
            Rows = rows,
            TotalRows = rows.Count,
            ValidRowCount = validRowCount,
            InvalidRowCount = rows.Count - validRowCount
        });
    }

    /// <summary>
    /// Converts a parsed row into its preview DTO, applying field-level validation on top of
    /// any parsing errors already collected by the parser.
    /// </summary>
    /// <param name="row">The parsed row to convert and validate.</param>
    /// <returns>The preview row DTO, including its combined error list.</returns>
    private CsvImportRowDto BuildRowDto(ImportRowParseResult row)
    {
        var candidate = new ImportEventItemDto
        {
            Title = row.Title ?? string.Empty,
            Date = row.Date ?? default,
            Description = row.Description,
            Location = row.Location ?? string.Empty,
            MaxCapacity = row.MaxCapacity ?? 0
        };

        var validationResult = _itemValidator.Validate(candidate);

        var errors = row.Errors
            .Concat(validationResult.Errors.Select(e => e.ErrorMessage))
            .Distinct()
            .ToList();

        return new CsvImportRowDto
        {
            RowNumber = row.RowNumber,
            Title = candidate.Title,
            Date = row.Date,
            Description = candidate.Description,
            Location = candidate.Location,
            MaxCapacity = row.MaxCapacity,
            SourceDay = row.SourceDay,
            SourceTime = row.SourceTime,
            SourceModality = row.SourceModality,
            SourceField = row.SourceField,
            SourceCategory = row.SourceCategory,
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }
}
