using FluentAssertions;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using SportsClubEventManager.Infrastructure.Logging;
using Xunit;

namespace SportsClubEventManager.Tests.Infrastructure.Logging;

/// <summary>
/// Unit tests for <see cref="SensitiveValueEnricher"/>, verifying that log event properties whose
/// name matches a sensitive pattern (password, secret, token, connection string, authorization)
/// are redacted, both when interpolated as a top-level scalar (<c>{Password}</c>) and when nested
/// inside a destructured object (<c>{@Command}</c>), while non-matching properties pass through
/// unchanged.
/// </summary>
/// <remarks>
/// Tests drive <see cref="SensitiveValueEnricher"/> through a real Serilog pipeline (a
/// <see cref="LoggerConfiguration"/> with the enricher attached and a sink that captures the
/// emitted <see cref="LogEvent"/>) rather than calling <c>Enrich</c> directly against a hand-built
/// <see cref="LogEvent"/>/<see cref="ILogEventPropertyFactory"/>. This exercises the exact
/// production code path — including Serilog's own message-template parsing of the <c>@</c>
/// destructuring operator and its real property-value construction — instead of a fake factory
/// whose behavior might not match Serilog's actual internals for pre-built property values.
///
/// <para>
/// <b>Known defect discovered while writing these tests (not fixed here — see the testing
/// summary/report for detail, per the instruction to flag rather than silently patch production
/// code):</b> <c>SensitiveValueEnricher.Enrich</c> (line ~34) calls
/// <c>propertyFactory.CreateProperty(property.Key, redactedValue)</c> where
/// <c>redactedValue</c> is already a built <see cref="LogEventPropertyValue"/> (a
/// <see cref="ScalarValue"/> or <see cref="StructureValue"/>). Serilog's real
/// <c>ILogEventPropertyFactory</c> implementation does not special-case an already-built
/// <see cref="LogEventPropertyValue"/> passed as <c>value</c>: since it is not a recognized
/// scalar type and destructuring defaults to <c>false</c>, it falls back to calling
/// <c>.ToString()</c> on it and wraps the result in a brand new <see cref="ScalarValue"/>. In
/// practice this means every test below that actually triggers redaction fails against the
/// current implementation:
/// <list type="bullet">
/// <item>Scalar case: the final value is a quoted rendering of the placeholder (a literal string
/// containing embedded quote characters, e.g. <c>"***REDACTED***"</c> with the quotes baked into
/// the .NET string) instead of the plain <c>***REDACTED***</c> text.</item>
/// <item>Destructured case: the whole nested <see cref="StructureValue"/> is collapsed into a
/// single <see cref="ScalarValue"/> (its <c>ToString()</c> rendering), destroying the structured/
/// nested JSON shape for that property entirely — not merely mis-formatting it.</item>
/// </list>
/// No plaintext secret is ever leaked either way (the placeholder text itself never contains the
/// original value), so this is not a data-exposure regression, but it does break the "logs are
/// JSON structured" and "value is exactly ***REDACTED***" guarantees the design document and this
/// class's own XML docs promise. The one-line fix (not applied, per this phase's constraints) is
/// to bypass the factory for this specific call, since <c>redactedValue</c> is already the right
/// type: <c>logEvent.AddOrUpdateProperty(new LogEventProperty(property.Key, redactedValue));</c>
/// </para>
/// </remarks>
public class SensitiveValueEnricherTests
{
    private const string RedactedValue = "***REDACTED***";

    private sealed record TestCommand(string CurrentPassword, string Email);

    private sealed record NestedCredentials(string Token);

    private sealed record CommandWithNestedObject(NestedCredentials Credentials, string UserId);

    /// <summary>
    /// A minimal <see cref="ILogEventSink"/> that captures every emitted <see cref="LogEvent"/> so
    /// tests can assert on its final (enriched) properties.
    /// </summary>
    private sealed class CapturingSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];

        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }

    /// <summary>
    /// Builds a real Serilog logger with <see cref="SensitiveValueEnricher"/> attached and a
    /// capturing sink, mirroring how <c>SerilogConfigurationExtensions.AddSerilogLogging</c> wires
    /// the enricher into the actual application pipeline.
    /// </summary>
    private static (Serilog.ILogger Logger, CapturingSink Sink) CreateLoggerWithEnricher()
    {
        var sink = new CapturingSink();
        var logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.With<SensitiveValueEnricher>()
            .WriteTo.Sink(sink)
            .CreateLogger();

        return (logger, sink);
    }

    /// <summary>
    /// Tests covering top-level scalar properties (e.g. <c>{Password}</c>) interpolated directly
    /// into a log message.
    /// </summary>
    public sealed class ScalarProperties : SensitiveValueEnricherTests
    {
        /// <summary>
        /// Verifies that a scalar property named exactly like a known sensitive pattern is
        /// redacted to the fixed placeholder value.
        /// </summary>
        [Theory]
        [InlineData("Password")]
        [InlineData("Secret")]
        [InlineData("Token")]
        [InlineData("ConnectionString")]
        [InlineData("Authorization")]
        public void Enrich_WhenScalarPropertyNameMatchesSensitivePattern_RedactsValue(string propertyName)
        {
            // Arrange
            var (logger, sink) = CreateLoggerWithEnricher();

            // Act
            logger.Information("Test message {" + propertyName + "}", "plain-text-value");

            // Assert
            var logEvent = sink.Events.Single();
            var value = logEvent.Properties[propertyName].Should().BeOfType<ScalarValue>().Subject;
            value.Value.Should().Be(RedactedValue);
        }

        /// <summary>
        /// Verifies that matching is case-insensitive, e.g. an all-uppercase property name still
        /// matches the "password" pattern.
        /// </summary>
        [Fact]
        public void Enrich_WhenScalarPropertyNameDiffersOnlyInCase_StillRedactsValue()
        {
            // Arrange
            var (logger, sink) = CreateLoggerWithEnricher();

            // Act
            logger.Information("Test message {PASSWORD}", "plain-text-value");

            // Assert
            var logEvent = sink.Events.Single();
            var value = logEvent.Properties["PASSWORD"].Should().BeOfType<ScalarValue>().Subject;
            value.Value.Should().Be(RedactedValue);
        }

        /// <summary>
        /// Verifies that matching is substring-based, so a property whose name merely contains a
        /// sensitive pattern (e.g. "CurrentPassword" contains "password") is also redacted.
        /// </summary>
        [Fact]
        public void Enrich_WhenScalarPropertyNameContainsSensitivePatternAsSubstring_RedactsValue()
        {
            // Arrange
            var (logger, sink) = CreateLoggerWithEnricher();

            // Act
            logger.Information("Test message {CurrentPassword}", "old-secret");

            // Assert
            var logEvent = sink.Events.Single();
            var value = logEvent.Properties["CurrentPassword"].Should().BeOfType<ScalarValue>().Subject;
            value.Value.Should().Be(RedactedValue);
        }

        /// <summary>
        /// Verifies that a scalar property whose name does not match any sensitive pattern is left
        /// completely unchanged.
        /// </summary>
        [Fact]
        public void Enrich_WhenScalarPropertyNameDoesNotMatch_LeavesValueUnchanged()
        {
            // Arrange
            var (logger, sink) = CreateLoggerWithEnricher();

            // Act
            logger.Information("Test message {UserId}", "user-123");

            // Assert
            var logEvent = sink.Events.Single();
            var value = logEvent.Properties["UserId"].Should().BeOfType<ScalarValue>().Subject;
            value.Value.Should().Be("user-123");
        }
    }

    /// <summary>
    /// Tests covering properties destructured with <c>@</c> (represented internally as a
    /// <see cref="StructureValue"/>), whose nested properties must also be inspected recursively.
    /// </summary>
    public sealed class DestructuredProperties : SensitiveValueEnricherTests
    {
        /// <summary>
        /// Verifies that a sensitive field nested inside a destructured object is redacted while a
        /// sibling non-sensitive field is left unchanged.
        /// </summary>
        [Fact]
        public void Enrich_WhenDestructuredObjectHasSensitiveNestedProperty_RedactsOnlyThatProperty()
        {
            // Arrange
            var (logger, sink) = CreateLoggerWithEnricher();
            var command = new TestCommand("old-secret", "user@example.com");

            // Act
            logger.Debug("Handling {@Request}", command);

            // Assert
            var logEvent = sink.Events.Single();
            var structure = logEvent.Properties["Request"].Should().BeOfType<StructureValue>().Subject;
            var redactedProperty = structure.Properties.Single(p => p.Name == nameof(TestCommand.CurrentPassword));
            var untouchedProperty = structure.Properties.Single(p => p.Name == nameof(TestCommand.Email));

            ((ScalarValue)redactedProperty.Value).Value.Should().Be(RedactedValue);
            ((ScalarValue)untouchedProperty.Value).Value.Should().Be("user@example.com");
        }

        /// <summary>
        /// Verifies that a destructured object with no sensitive nested properties is left
        /// structurally unchanged (same nested values, none replaced).
        /// </summary>
        [Fact]
        public void Enrich_WhenDestructuredObjectHasNoSensitiveNestedProperties_LeavesStructureUnchanged()
        {
            // Arrange
            var (logger, sink) = CreateLoggerWithEnricher();

            // Act
            logger.Debug("Handling {@Request}", new { UserId = "user-123", Email = "user@example.com" });

            // Assert
            var logEvent = sink.Events.Single();
            var structure = logEvent.Properties["Request"].Should().BeOfType<StructureValue>().Subject;
            ((ScalarValue)structure.Properties.Single(p => p.Name == "UserId").Value).Value.Should().Be("user-123");
            ((ScalarValue)structure.Properties.Single(p => p.Name == "Email").Value).Value
                .Should().Be("user@example.com");
        }

        /// <summary>
        /// Verifies that redaction recurses into a destructured object nested inside another
        /// destructured object, not just one level deep — required because
        /// <see cref="Application.Common.Behaviors.LoggingBehavior{TRequest,TResponse}"/>
        /// destructures whole MediatR request objects, which may themselves contain nested value
        /// objects/DTOs carrying a sensitive field.
        /// </summary>
        [Fact]
        public void Enrich_WhenSensitivePropertyIsNestedTwoLevelsDeep_StillRedactsIt()
        {
            // Arrange
            var (logger, sink) = CreateLoggerWithEnricher();
            var command = new CommandWithNestedObject(new NestedCredentials("deeply-nested-secret"), "user-123");

            // Act
            logger.Debug("Handling {@Request}", command);

            // Assert
            var logEvent = sink.Events.Single();
            var outerStructure = logEvent.Properties["Request"].Should().BeOfType<StructureValue>().Subject;
            var innerStructure = outerStructure.Properties
                .Single(p => p.Name == nameof(CommandWithNestedObject.Credentials)).Value
                .Should().BeOfType<StructureValue>().Subject;
            ((ScalarValue)innerStructure.Properties.Single(p => p.Name == nameof(NestedCredentials.Token)).Value)
                .Value.Should().Be(RedactedValue);
            ((ScalarValue)outerStructure.Properties.Single(p => p.Name == "UserId").Value).Value
                .Should().Be("user-123");
        }
    }
}
