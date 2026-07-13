using System.ComponentModel.DataAnnotations;

namespace SportsClubEventManager.Infrastructure.Configuration;

/// <summary>
/// Strongly typed representation of the "Notifications:N8n" configuration section. Validated at
/// startup by <see cref="N8nOptionsValidator"/> — validation only applies when <see cref="Enabled"/>
/// is <c>true</c>, since the whole integration is disabled by default outside production (issue #37,
/// no project-owned n8n instance exists in development).
/// </summary>
public sealed class N8nOptions
{
    /// <summary>
    /// The configuration section name this options class binds to.
    /// </summary>
    public const string SectionName = "Notifications:N8n";

    /// <summary>
    /// Master switch for the n8n integration. Defaults to <c>false</c>; only set to <c>true</c> in
    /// production, once the manual runbook in the design doc's "Apéndice A" has been executed.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// Gets the n8n webhook URL that triggers the "registration confirmed" workflow.
    /// </summary>
    [Url]
    public string RegistrationConfirmedWebhookUrl { get; init; } = string.Empty;

    /// <summary>
    /// Gets the n8n webhook URL that triggers the "event updated" workflow.
    /// </summary>
    [Url]
    public string EventUpdatedWebhookUrl { get; init; } = string.Empty;

    /// <summary>
    /// Gets the n8n webhook URL that triggers the "event cancelled" workflow.
    /// </summary>
    [Url]
    public string EventCancelledWebhookUrl { get; init; } = string.Empty;

    /// <summary>
    /// Gets the n8n webhook URL that triggers the "event reminder" workflow.
    /// </summary>
    [Url]
    public string EventReminderWebhookUrl { get; init; } = string.Empty;

    /// <summary>
    /// Shared-secret token sent as the "X-N8n-Webhook-Token" header on every outbound call,
    /// validated by a native n8n Header Auth credential on each imported Webhook Trigger node.
    /// </summary>
    public string WebhookToken { get; init; } = string.Empty;

    /// <summary>
    /// Per-request timeout for outbound calls to n8n. Kept short and bounded so a slow/unreachable
    /// n8n instance never meaningfully delays the HTTP response of the business operation that
    /// triggered the notification.
    /// </summary>
    [Range(1, 60)]
    public int TimeoutSeconds { get; init; } = 5;

    /// <summary>
    /// The reminder intervals (in hours before the event start) that EventReminderBackgroundService
    /// evaluates on every poll — e.g. [24, 1] sends a reminder 24h and 1h before each event.
    /// </summary>
    public int[] ReminderIntervalHours { get; init; } = [24, 1];

    /// <summary>
    /// How often EventReminderBackgroundService polls for due reminders.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int PollingIntervalMinutes { get; init; } = 5;
}
