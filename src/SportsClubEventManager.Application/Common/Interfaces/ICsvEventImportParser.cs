using SportsClubEventManager.Application.Import.Models;

namespace SportsClubEventManager.Application.Common.Interfaces;

/// <summary>
/// Defines the contract for parsing a standardized event-calendar CSV file
/// (header: "DÍA,MODAL.,NOMBRE TIRADA,HORA,CAMPO,LUGAR,CAT") into rows mapped to
/// the writable <c>Event</c> fields.
/// </summary>
public interface ICsvEventImportParser
{
    /// <summary>
    /// Parses a CSV stream into structured rows mapped to <c>Event</c> fields.
    /// </summary>
    /// <param name="csvStream">The uploaded CSV file content.</param>
    /// <param name="columnMapping">
    /// An optional mapping from the column headers actually found in the file to the
    /// standardized column names, used when headers do not match exactly.
    /// </param>
    /// <param name="defaultMaxCapacity">
    /// The default value applied to every row's <c>MaxCapacity</c>, since the standardized
    /// schema has no source column for it.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The parse result, containing detected headers, mapped rows, and any fatal error.</returns>
    CsvParseResult Parse(
        Stream csvStream,
        IReadOnlyDictionary<string, string>? columnMapping,
        int defaultMaxCapacity,
        CancellationToken cancellationToken);
}
