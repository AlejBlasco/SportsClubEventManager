using System.ComponentModel.DataAnnotations;

namespace SportsClubEventManager.Infrastructure.Configuration;

/// <summary>
/// Strongly typed representation of the "Metrics" configuration section, validated at startup
/// via <c>ValidateDataAnnotations().ValidateOnStart()</c>.
/// </summary>
public sealed class MetricsOptions
{
    /// <summary>
    /// The configuration section name this options class binds to.
    /// </summary>
    public const string SectionName = "Metrics";

    /// <summary>
    /// Gets the interval, in seconds, at which
    /// <see cref="SportsClubEventManager.Infrastructure.Metrics.ActiveEventsGaugeUpdater"/>
    /// recomputes the "active events" gauge.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int ActiveEventsRefreshIntervalSeconds { get; init; } = 30;
}
