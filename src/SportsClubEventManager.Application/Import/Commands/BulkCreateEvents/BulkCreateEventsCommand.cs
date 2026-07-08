using MediatR;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.Application.Import.Commands.BulkCreateEvents;

/// <summary>
/// Command to confirm a CSV import, inserting all admin-approved event rows in a single,
/// all-or-nothing database transaction.
/// </summary>
public sealed class BulkCreateEventsCommand : IRequest<CsvImportResultDto>
{
    /// <summary>
    /// Gets or sets the ID of the administrator confirming the import.
    /// </summary>
    public required Guid AdminUserId { get; init; }

    /// <summary>
    /// Gets or sets the admin-approved, already-mapped event rows to import.
    /// </summary>
    public required IReadOnlyList<ImportEventItemDto> Events { get; init; }

    /// <summary>
    /// Gets or sets the IP address of the administrator, if available.
    /// </summary>
    public string? IpAddress { get; init; }

    /// <summary>
    /// Gets or sets the User-Agent string of the client, if available.
    /// </summary>
    public string? UserAgent { get; init; }
}
