using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Application.Common.Models.Notifications;
using SportsClubEventManager.Infrastructure.Configuration;

namespace SportsClubEventManager.Infrastructure.Notifications;

/// <summary>
/// HTTP implementation of <see cref="IWorkflowNotifier"/> that POSTs JSON payloads to n8n's
/// per-workflow webhook trigger URLs. Every public method swallows and logs its own exceptions —
/// a failure to reach n8n must never fail the business operation that triggered the notification
/// (same reliability principle already applied by ActiveEventsGaugeUpdater, issue #42).
/// </summary>
public sealed class N8nWorkflowNotifier(
    IHttpClientFactory httpClientFactory,
    IOptions<N8nOptions> options,
    IApplicationMetrics metrics,
    ILogger<N8nWorkflowNotifier> logger) : IWorkflowNotifier
{
    /// <summary>
    /// The n8n workflow expressions (e.g. <c>$json.body.UserEmail</c>) match the payload records'
    /// PascalCase property names as exported from the n8n UI. <see cref="JsonContent.Create{T}(T, System.Net.Http.Headers.MediaTypeHeaderValue?, JsonSerializerOptions?)"/>
    /// defaults to <see cref="JsonSerializerDefaults.Web"/> (camelCase) when no options are given,
    /// which would silently break every field lookup in the workflows — so PascalCase is preserved
    /// explicitly here instead.
    /// </summary>
    private static readonly JsonSerializerOptions PayloadSerializerOptions = new();


    /// <inheritdoc />
    public Task NotifyRegistrationConfirmedAsync(RegistrationConfirmedPayload payload, CancellationToken cancellationToken) =>
        PostAsync("registration-confirmed", options.Value.RegistrationConfirmedWebhookUrl, payload, cancellationToken);

    /// <inheritdoc />
    public Task NotifyEventUpdatedAsync(EventChangedPayload payload, CancellationToken cancellationToken) =>
        PostAsync("event-updated", options.Value.EventUpdatedWebhookUrl, payload, cancellationToken);

    /// <inheritdoc />
    public Task NotifyEventCancelledAsync(EventChangedPayload payload, CancellationToken cancellationToken) =>
        PostAsync("event-cancelled", options.Value.EventCancelledWebhookUrl, payload, cancellationToken);

    /// <inheritdoc />
    public Task NotifyEventReminderAsync(EventReminderPayload payload, CancellationToken cancellationToken) =>
        PostAsync("event-reminder", options.Value.EventReminderWebhookUrl, payload, cancellationToken);

    /// <summary>
    /// Posts the given payload to the given n8n webhook URL, tagging the outcome with
    /// <paramref name="workflow"/> for logging/metrics. Never throws.
    /// </summary>
    /// <param name="workflow">Bounded label identifying which workflow is being triggered.</param>
    /// <param name="webhookUrl">The n8n webhook trigger URL to POST to.</param>
    /// <param name="payload">The JSON payload to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task PostAsync<TPayload>(string workflow, string webhookUrl, TPayload payload, CancellationToken cancellationToken)
    {
        if (!options.Value.Enabled)
        {
            // Disabled by default in every environment except production (see design doc,
            // "Architecture Overview" — no project-owned n8n instance exists outside production).
            return;
        }

        try
        {
            var client = httpClientFactory.CreateClient("N8n");
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(options.Value.TimeoutSeconds));

            using var request = new HttpRequestMessage(HttpMethod.Post, webhookUrl)
            {
                Content = JsonContent.Create(payload, options: PayloadSerializerOptions)
            };
            request.Headers.Add("X-N8n-Webhook-Token", options.Value.WebhookToken);

            var response = await client.SendAsync(request, cts.Token);
            var success = response.IsSuccessStatusCode;

            if (!success)
            {
                logger.LogWarning(
                    "n8n workflow {Workflow} returned a non-success status code {StatusCode}.",
                    workflow, (int)response.StatusCode);
            }

            metrics.RecordWorkflowNotificationSent(workflow, success);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // Network failure or timeout — logged and counted, never rethrown (see class summary).
            logger.LogWarning(ex, "Failed to notify n8n workflow {Workflow}.", workflow);
            metrics.RecordWorkflowNotificationSent(workflow, success: false);
        }
    }
}
