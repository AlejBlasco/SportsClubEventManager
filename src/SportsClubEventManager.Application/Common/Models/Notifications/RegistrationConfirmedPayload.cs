namespace SportsClubEventManager.Application.Common.Models.Notifications;

/// <summary>
/// Payload describing a single event registration, sent to n8n's "registration confirmed" workflow.
/// </summary>
public sealed record RegistrationConfirmedPayload
{
    /// <summary>
    /// Gets the identifier of the event the user registered for.
    /// </summary>
    public required Guid EventId { get; init; }

    /// <summary>
    /// Gets the title of the event.
    /// </summary>
    public required string EventTitle { get; init; }

    /// <summary>
    /// Gets the date and time the event takes place.
    /// </summary>
    public required DateTime EventDate { get; init; }

    /// <summary>
    /// Gets the location where the event takes place.
    /// </summary>
    public required string Location { get; init; }

    /// <summary>
    /// Gets the email address of the user who registered.
    /// </summary>
    public required string UserEmail { get; init; }

    /// <summary>
    /// Gets the display name of the user who registered.
    /// </summary>
    public required string UserName { get; init; }
}
