namespace SportsClubEventManager.Application.Common.Models.Notifications;

/// <summary>
/// Payload describing an event-level change (update or cancellation), including every currently
/// active registrant so n8n's "Send Email" node can loop over them. Reuses the same recipient list
/// already loaded by UpdateEventCommandHandler/DeleteEventCommandHandler (no extra query needed
/// beyond adding ThenInclude(r => r.User) to their existing Include(e => e.Registrations)).
/// </summary>
public sealed record EventChangedPayload
{
    /// <summary>
    /// Gets the identifier of the event that changed.
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
    /// Gets the kind of change that occurred: "updated" or "cancelled".
    /// </summary>
    public required string ChangeType { get; init; }

    /// <summary>
    /// Gets the list of actively registered users to notify about the change.
    /// </summary>
    public required IReadOnlyList<NotificationRecipient> Recipients { get; init; }
}
