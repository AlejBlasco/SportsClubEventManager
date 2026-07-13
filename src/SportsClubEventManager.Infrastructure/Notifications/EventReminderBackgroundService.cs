using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Application.Common.Models.Notifications;
using SportsClubEventManager.Domain.Entities;
using SportsClubEventManager.Domain.Enums;
using SportsClubEventManager.Infrastructure.Configuration;

namespace SportsClubEventManager.Infrastructure.Notifications;

/// <summary>
/// Periodically checks for events entering one of the configured reminder windows (e.g. 24h/1h
/// before start) and notifies n8n once per (event, interval) pair — persisted via
/// EventReminderNotification so a reminder is never sent twice, including across process restarts.
/// Same BackgroundService + PeriodicTimer + per-iteration IServiceScope pattern already used by
/// ActiveEventsGaugeUpdater (issue #42).
/// </summary>
public sealed class EventReminderBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<N8nOptions> options,
    ILogger<EventReminderBackgroundService> logger) : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(options.Value.PollingIntervalMinutes));
        do
        {
            try
            {
                await ProcessDueRemindersAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // A failed tick must not crash the background service — the next tick retries.
                logger.LogWarning(ex, "Failed to process due event reminders.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    /// <summary>
    /// Evaluates every configured reminder interval, notifies n8n for each event that just entered
    /// that interval's window and has not been notified yet, and records the attempt so it is never
    /// repeated. Extracted as an internal method so it can be unit tested without needing a running
    /// BackgroundService or PeriodicTimer.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    internal async Task ProcessDueRemindersAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var notifier = scope.ServiceProvider.GetRequiredService<IWorkflowNotifier>();

        var now = DateTime.UtcNow;

        foreach (var intervalHours in options.Value.ReminderIntervalHours)
        {
            var windowEnd = now.AddHours(intervalHours);

            var dueEvents = await context.Events
                .Include(e => e.Registrations.Where(r => r.Status != RegistrationStatus.Cancelled))
                .ThenInclude(r => r.User)
                .Where(e => e.Date >= now && e.Date <= windowEnd)
                .Where(e => !context.EventReminderNotifications
                    .Any(n => n.EventId == e.Id && n.IntervalHours == intervalHours))
                .ToListAsync(cancellationToken);

            foreach (var eventEntity in dueEvents)
            {
                var payload = new EventReminderPayload
                {
                    EventId = eventEntity.Id,
                    EventTitle = eventEntity.Title,
                    EventDate = eventEntity.Date,
                    Location = eventEntity.Location,
                    IntervalHours = intervalHours,
                    Recipients = eventEntity.Registrations
                        .Select(r => new NotificationRecipient { Email = r.User.Email, Name = r.User.Name })
                        .ToList()
                };

                await notifier.NotifyEventReminderAsync(payload, cancellationToken);

                // Recorded after the notification attempt (see design doc, "Riesgos y Decisiones
                // Abiertas" for the accepted trade-off of recording it regardless of the outbound
                // call's success/failure, to avoid retrying indefinitely against a workflow n8n
                // itself already reports as failed in its own Executions log).
                context.EventReminderNotifications.Add(new EventReminderNotification
                {
                    EventId = eventEntity.Id,
                    IntervalHours = intervalHours,
                    SentAtUtc = now
                });
            }

            if (dueEvents.Count > 0)
            {
                await context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
