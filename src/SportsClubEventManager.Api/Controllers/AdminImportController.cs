using System.Security.Claims;
using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SportsClubEventManager.Application.Common.Constants;
using SportsClubEventManager.Application.Import.Commands.BulkCreateEvents;
using SportsClubEventManager.Application.Import.Commands.ParseCsvFile;
using SportsClubEventManager.Domain.Exceptions;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Api.Controllers;

/// <summary>
/// API controller for CSV event import operations (Administrator only): template download,
/// dry-run preview, and confirm.
/// </summary>
[ApiController]
[Route("api/admin/import")]
[Authorize(Roles = "Administrator")]
public sealed class AdminImportController(ISender sender, IConfiguration configuration) : ControllerBase
{
    private static readonly JsonSerializerOptions ColumnMappingJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private const string TemplateCsvContent =
        "DÍA,MODAL.,NOMBRE TIRADA,HORA,CAMPO,LUGAR,CAT\r\n" +
        "15/09/2026,Trap,1ª Tirada El Balín,10:00,Campo 2,Club de Tiro Norte,S1\r\n";

    /// <summary>
    /// Downloads a CSV template using the standardized import header, with one example row.
    /// </summary>
    /// <returns>The template file content.</returns>
    [HttpGet("template")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult DownloadTemplate()
    {
        // Emit a UTF-8 BOM so the accented "DÍA" header opens correctly in spreadsheet apps.
        var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(TemplateCsvContent);
        return File(bytes, "text/csv", "event-import-template.csv");
    }

    /// <summary>
    /// Parses an uploaded CSV file into a preview of mapped event rows. Performs no database
    /// writes. Structural failures (unreadable file, missing required columns, oversized file)
    /// are returned as a 200 response with <see cref="CsvImportPreviewResponse.FatalError"/>
    /// populated, rather than an error status, so the UI can render a friendly inline message.
    /// </summary>
    /// <param name="file">The uploaded CSV file.</param>
    /// <param name="columnMapping">Optional JSON-serialized list of <see cref="ImportColumnMappingDto"/> entries.</param>
    /// <param name="defaultMaxCapacity">Optional override for the default maximum capacity applied to every row.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The preview response.</returns>
    [HttpPost("csv/preview")]
    [RequestFormLimits(MultipartBodyLengthLimit = 10_485_760)]
    [ProducesResponseType(typeof(CsvImportPreviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> PreviewCsv(
        [FromForm] IFormFile? file,
        [FromForm] string? columnMapping,
        [FromForm] int? defaultMaxCapacity,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return Ok(new CsvImportPreviewResponse { FatalError = "No file was uploaded." });
        }

        if (!string.Equals(Path.GetExtension(file.FileName), ".csv", StringComparison.OrdinalIgnoreCase))
        {
            return Ok(new CsvImportPreviewResponse { FatalError = "Only .csv files are supported." });
        }

        var maxFileSizeBytes = configuration.GetValue(ImportSettingsKeys.MaxFileSizeBytes, ImportSettingsKeys.MaxFileSizeBytesFallback);

        if (file.Length > maxFileSizeBytes)
        {
            return Ok(new CsvImportPreviewResponse
            {
                FatalError = $"The file exceeds the maximum allowed size of {maxFileSizeBytes / (1024 * 1024)} MB."
            });
        }

        Dictionary<string, string>? mapping = null;

        if (!string.IsNullOrWhiteSpace(columnMapping))
        {
            try
            {
                var entries = JsonSerializer.Deserialize<List<ImportColumnMappingDto>>(columnMapping, ColumnMappingJsonOptions);
                mapping = entries?.ToDictionary(e => e.SourceColumn, e => e.TargetColumn, StringComparer.OrdinalIgnoreCase);
            }
            catch (JsonException)
            {
                return Ok(new CsvImportPreviewResponse { FatalError = "The column mapping payload is not valid JSON." });
            }
        }

        await using var stream = file.OpenReadStream();

        var command = new ParseCsvFileCommand
        {
            FileStream = stream,
            FileName = file.FileName,
            ColumnMapping = mapping,
            DefaultMaxCapacity = defaultMaxCapacity
        };

        var result = await sender.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Confirms a CSV import, inserting all admin-approved rows in a single, all-or-nothing transaction.
    /// </summary>
    /// <param name="request">The admin-approved, already-mapped event rows.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The import result.</returns>
    [HttpPost("csv")]
    [ProducesResponseType(typeof(CsvImportResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ConfirmImport(
        [FromBody] ConfirmCsvImportRequest request,
        CancellationToken cancellationToken)
    {
        var adminUserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (adminUserIdClaim is null || !Guid.TryParse(adminUserIdClaim, out var adminUserId))
        {
            return Unauthorized();
        }

        try
        {
            var command = new BulkCreateEventsCommand
            {
                AdminUserId = adminUserId,
                Events = request.Events,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = HttpContext.Request.Headers.UserAgent.ToString()
            };

            var result = await sender.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
