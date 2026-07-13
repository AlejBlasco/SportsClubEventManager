namespace SportsClubEventManager.Application.Common.Models.Notifications;

/// <summary>
/// Payload describing a due reminder for an upcoming event.
/// </summary>
public sealed record EventReminderPayload
{
    /// <summary>
    /// Gets the identifier of the event the reminder is for.
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
    /// Gets the reminder interval, in hours before the event start, that triggered this notification.
    /// </summary>
    public required int IntervalHours { get; init; }

    /// <summary>
    /// Gets the list of actively registered users to notify about the reminder.
    /// </summary>
    public required IReadOnlyList<NotificationRecipient> Recipients { get; init; }
}
