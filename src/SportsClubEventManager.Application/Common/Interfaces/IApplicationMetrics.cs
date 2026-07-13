namespace SportsClubEventManager.Application.Common.Interfaces;

/// <summary>
/// Abstraction over business-metric recording. Implemented in Infrastructure using
/// prometheus-net, so Application stays free of any third-party metrics dependency
/// (same pattern already used for IAuditService / IDateTimeProvider).
/// </summary>
public interface IApplicationMetrics
{
    /// <summary>
    /// Records that a new event registration was created.
    /// </summary>
    /// <param name="source">Bounded label: "self-service" or "admin" — who created the registration.</param>
    void RecordRegistrationCreated(string source);

    /// <summary>
    /// Records that an event registration was cancelled.
    /// </summary>
    /// <param name="source">Bounded label: "self-service" or "admin" — who cancelled the registration.</param>
    void RecordRegistrationCancelled(string source);

    /// <summary>
    /// Records the outcome of an attempt to notify n8n about a workflow-relevant event.
    /// </summary>
    /// <param name="workflow">Bounded label: "registration-confirmed" | "event-updated" | "event-cancelled" | "event-reminder".</param>
    /// <param name="success"><c>true</c> if the outbound HTTP call to n8n succeeded (2xx response); otherwise <c>false</c>.</param>
    void RecordWorkflowNotificationSent(string workflow, bool success);
}
