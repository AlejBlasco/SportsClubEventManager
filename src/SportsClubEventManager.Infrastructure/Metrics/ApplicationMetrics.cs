using Prometheus;
using SportsClubEventManager.Application.Common.Interfaces;

namespace SportsClubEventManager.Infrastructure.Metrics;

/// <summary>
/// prometheus-net implementation of <see cref="IApplicationMetrics"/>. Registered as a
/// singleton — Counter instances must be created once per process and reused.
/// </summary>
public sealed class ApplicationMetrics : IApplicationMetrics
{
    private static readonly Counter RegistrationsCreated = Prometheus.Metrics.CreateCounter(
        "sportsclubeventmanager_event_registrations_total",
        "Total number of event registrations created.",
        new CounterConfiguration { LabelNames = ["source"] });

    private static readonly Counter RegistrationsCancelled = Prometheus.Metrics.CreateCounter(
        "sportsclubeventmanager_registration_cancellations_total",
        "Total number of event registrations cancelled.",
        new CounterConfiguration { LabelNames = ["source"] });

    private static readonly Counter WorkflowNotificationsSent = Prometheus.Metrics.CreateCounter(
        "sportsclubeventmanager_workflow_notifications_total",
        "Total number of n8n workflow notification attempts, by workflow and outcome.",
        new CounterConfiguration { LabelNames = ["workflow", "result"] });

    /// <inheritdoc />
    public void RecordRegistrationCreated(string source) =>
        RegistrationsCreated.WithLabels(source).Inc();

    /// <inheritdoc />
    public void RecordRegistrationCancelled(string source) =>
        RegistrationsCancelled.WithLabels(source).Inc();

    /// <inheritdoc />
    public void RecordWorkflowNotificationSent(string workflow, bool success) =>
        WorkflowNotificationsSent.WithLabels(workflow, success ? "success" : "failure").Inc();
}
