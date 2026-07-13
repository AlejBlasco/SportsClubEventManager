using SportsClubEventManager.Domain.Common;

namespace SportsClubEventManager.Domain.Entities;

/// <summary>
/// Records that a reminder notification for a given event and interval has already been sent,
/// so <c>SportsClubEventManager.Infrastructure.Notifications.EventReminderBackgroundService</c>
/// (Infrastructure) never sends the same reminder twice across polling ticks or process restarts.
/// </summary>
public class EventReminderNotification : BaseEntity
{
    /// <summary>
    /// Gets or sets the identifier of the event this reminder was sent for.
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Gets or sets the reminder interval, in hours before the event start, that was sent
    /// (e.g. 24 or 1 — matches one of the configured values in Notifications:N8n:ReminderIntervalHours).
    /// </summary>
    public int IntervalHours { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the reminder notification was successfully sent to n8n.
    /// </summary>
    public DateTime SentAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the event this reminder is associated with.
    /// </summary>
    public Event Event { get; set; } = null!;
}
