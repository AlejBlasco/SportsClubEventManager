using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Prometheus;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Infrastructure.Configuration;

namespace SportsClubEventManager.Infrastructure.Metrics;

/// <summary>
/// Periodically recomputes the "active events" gauge (events whose date has not yet
/// occurred — same definition of "active" already used by RegisterForEventCommandHandler /
/// CancelRegistrationCommandHandler when validating eventEntity.Date against UtcNow).
/// </summary>
public sealed class ActiveEventsGaugeUpdater(
    IServiceScopeFactory scopeFactory,
    IOptions<MetricsOptions> options,
    ILogger<ActiveEventsGaugeUpdater> logger) : BackgroundService
{
    private static readonly Gauge ActiveEvents = Prometheus.Metrics.CreateGauge(
        "sportsclubeventmanager_active_events",
        "Current number of events whose date has not yet occurred.");

    private readonly TimeSpan _interval = TimeSpan.FromSeconds(options.Value.ActiveEventsRefreshIntervalSeconds);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval);
        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                var count = await GetActiveEventsCountAsync(context, stoppingToken);
                ActiveEvents.Set(count);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // A failed refresh must not crash the background service; the gauge simply
                // keeps its last known value until the next successful tick.
                logger.LogWarning(ex, "Failed to refresh the active events gauge.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    /// <summary>
    /// Counts the number of events whose date has not yet occurred. Extracted as an internal
    /// static method so it can be unit tested without needing a running BackgroundService or
    /// PeriodicTimer.
    /// </summary>
    /// <param name="context">The application database context to query events from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of events whose <c>Date</c> is greater than or equal to the current UTC time.</returns>
    internal static Task<int> GetActiveEventsCountAsync(
        IApplicationDbContext context, CancellationToken cancellationToken) =>
        context.Events.CountAsync(e => e.Date >= DateTime.UtcNow, cancellationToken);
}
