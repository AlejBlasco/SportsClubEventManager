using SportsClubEventManager.Domain.Enums;

namespace SportsClubEventManager.Shared.DTOs;

/// <summary>
/// Data transfer object returned when a registration is successfully created.
/// Includes full event details for client convenience.
/// </summary>
public sealed record RegistrationCreatedDto
{
    /// <summary>
    /// Gets the unique identifier of the registration.
    /// </summary>
    public required Guid RegistrationId { get; init; }

    /// <summary>
    /// Gets the unique identifier of the event.
    /// </summary>
    public required Guid EventId { get; init; }

    /// <summary>
    /// Gets the unique identifier of the user who registered.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// Gets the date and time when the registration was created.
    /// </summary>
    public required DateTime RegisteredAt { get; init; }

    /// <summary>
    /// Gets the status of the registration.
    /// </summary>
    public required RegistrationStatus Status { get; init; }

    /// <summary>
    /// Gets the full event details including current capacity information.
    /// </summary>
    public required EventDetailDto Event { get; init; }
}
