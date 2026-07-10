using Serilog.Core;
using Serilog.Events;

namespace SportsClubEventManager.Infrastructure.Logging;

/// <summary>
/// Serilog enricher that redacts the value of any log event property whose name matches a
/// known sensitive pattern (password, secret, token, connection string, authorization). Acts as
/// a safety net for the "sensitive data is never logged" requirement, independent of whether the
/// value was interpolated as a scalar (<c>{Password}</c>) or nested inside a destructured object
/// (<c>{@Command}</c>).
/// </summary>
public sealed class SensitiveValueEnricher : ILogEventEnricher
{
    private const string RedactedValue = "***REDACTED***";

    private static readonly string[] SensitiveNamePatterns =
        ["password", "secret", "token", "connectionstring", "authorization"];

    /// <summary>
    /// Inspects every property of the given log event and replaces the value of any property
    /// (including properties nested inside destructured objects) whose name matches a known
    /// sensitive pattern with a fixed redacted placeholder.
    /// </summary>
    /// <param name="logEvent">The log event about to be emitted.</param>
    /// <param name="propertyFactory">Factory used to create the replacement properties.</param>
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        foreach (var property in logEvent.Properties)
        {
            var redactedValue = RedactIfNeeded(property.Key, property.Value);
            if (redactedValue is not null)
            {
                logEvent.AddOrUpdateProperty(new LogEventProperty(property.Key, redactedValue));
            }
        }
    }

    /// <summary>
    /// Returns a redacted replacement for <paramref name="value"/> when <paramref name="propertyName"/>
    /// itself matches a sensitive pattern, or when <paramref name="value"/> is a destructured object
    /// containing nested properties that need redaction; returns <c>null</c> when nothing changes.
    /// </summary>
    private static LogEventPropertyValue? RedactIfNeeded(string propertyName, LogEventPropertyValue value)
    {
        if (IsSensitiveName(propertyName))
        {
            return new ScalarValue(RedactedValue);
        }

        return value is StructureValue structureValue ? RedactStructure(structureValue) : null;
    }

    /// <summary>
    /// Recurses into a destructured object's properties, redacting any nested property whose
    /// name matches a sensitive pattern; returns <c>null</c> when no nested property changed.
    /// </summary>
    private static StructureValue? RedactStructure(StructureValue structureValue)
    {
        var changed = false;
        var properties = new List<LogEventProperty>(structureValue.Properties.Count);

        foreach (var property in structureValue.Properties)
        {
            var redactedValue = RedactIfNeeded(property.Name, property.Value);
            if (redactedValue is not null)
            {
                changed = true;
                properties.Add(new LogEventProperty(property.Name, redactedValue));
            }
            else
            {
                properties.Add(property);
            }
        }

        return changed ? new StructureValue(properties, structureValue.TypeTag) : null;
    }

    private static bool IsSensitiveName(string propertyName) =>
        SensitiveNamePatterns.Any(pattern => propertyName.Contains(pattern, StringComparison.OrdinalIgnoreCase));
}
