using SportsClubEventManager.Application.Common.Models.Notifications;

namespace SportsClubEventManager.Application.Common.Interfaces;

/// <summary>
/// Abstraction over triggering external workflow-automation notifications for event lifecycle
/// changes. Implemented in Infrastructure using an outbound HTTP call to n8n, so Application
/// stays free of any HTTP/third-party dependency (same pattern already used for IApplicationMetrics
/// / IAuditService). Every method is designed to never throw: implementations must swallow and log
/// their own failures, so a notification failure never rolls back or fails the business operation
/// that triggered it.
/// </summary>
public interface IWorkflowNotifier
{
    /// <summary>
    /// Notifies that a user successfully registered for an event.
    /// </summary>
    /// <param name="payload">The registration details to notify about.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task NotifyRegistrationConfirmedAsync(RegistrationConfirmedPayload payload, CancellationToken cancellationToken);

    /// <summary>
    /// Notifies that an event's details (time, location, capacity, etc.) were updated.
    /// </summary>
    /// <param name="payload">The event change details to notify about.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task NotifyEventUpdatedAsync(EventChangedPayload payload, CancellationToken cancellationToken);

    /// <summary>
    /// Notifies that an event was cancelled (deleted).
    /// </summary>
    /// <param name="payload">The event change details to notify about.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task NotifyEventCancelledAsync(EventChangedPayload payload, CancellationToken cancellationToken);

    /// <summary>
    /// Notifies that an event is starting within one of the configured reminder intervals.
    /// </summary>
    /// <param name="payload">The reminder details to notify about.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task NotifyEventReminderAsync(EventReminderPayload payload, CancellationToken cancellationToken);
}
