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
}
