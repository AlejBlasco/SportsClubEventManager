using FluentAssertions;
using Prometheus;
using SportsClubEventManager.Infrastructure.Metrics;
using Xunit;

namespace SportsClubEventManager.Tests.Infrastructure.Metrics;

/// <summary>
/// Unit tests for <see cref="ApplicationMetrics"/> verifying that the prometheus-net
/// <see cref="Counter"/> instances backing <see cref="ApplicationMetrics.RecordRegistrationCreated"/>
/// and <see cref="ApplicationMetrics.RecordRegistrationCancelled"/> are incremented correctly with
/// the expected "source" label. No mocking is used: prometheus-net's <see cref="Prometheus.Metrics.CreateCounter(string, string, CounterConfiguration)"/>
/// is idempotent for an already-registered metric name, so re-requesting the same counter by name
/// from the test returns the exact same underlying collector instance that <see cref="ApplicationMetrics"/>
/// uses internally, allowing the test to read <c>.Value</c> directly off it.
/// Each test uses a unique "source" label value (a fresh GUID) so tests can run in parallel/any
/// order without interfering with each other's counters, since prometheus-net's default registry
/// is a process-wide singleton shared across the whole test assembly.
/// </summary>
public class ApplicationMetricsTests
{
    private readonly ApplicationMetrics _metrics = new();

    private static Counter RegistrationsCreatedCounter() =>
        Prometheus.Metrics.CreateCounter(
            "sportsclubeventmanager_event_registrations_total",
            "Total number of event registrations created.",
            new CounterConfiguration { LabelNames = ["source"] });

    private static Counter RegistrationsCancelledCounter() =>
        Prometheus.Metrics.CreateCounter(
            "sportsclubeventmanager_registration_cancellations_total",
            "Total number of event registrations cancelled.",
            new CounterConfiguration { LabelNames = ["source"] });

    /// <summary>
    /// Tests covering <see cref="ApplicationMetrics.RecordRegistrationCreated"/>.
    /// </summary>
    public sealed class RecordRegistrationCreatedTests : ApplicationMetricsTests
    {
        /// <summary>
        /// Verifies that calling RecordRegistrationCreated increments the registrations-created
        /// counter for the given "source" label from zero to one.
        /// </summary>
        [Fact]
        public void RecordRegistrationCreated_WhenCalledOnce_IncrementsCounterForSourceLabelByOne()
        {
            // Arrange
            var source = $"self-service-{Guid.NewGuid()}";
            var counter = RegistrationsCreatedCounter();

            // Act
            _metrics.RecordRegistrationCreated(source);

            // Assert
            counter.WithLabels(source).Value.Should().Be(1);
        }

        /// <summary>
        /// Verifies that calling RecordRegistrationCreated multiple times accumulates the counter
        /// value for the same "source" label instead of resetting it.
        /// </summary>
        [Fact]
        public void RecordRegistrationCreated_WhenCalledMultipleTimes_AccumulatesCounterValue()
        {
            // Arrange
            var source = $"admin-{Guid.NewGuid()}";
            var counter = RegistrationsCreatedCounter();

            // Act
            _metrics.RecordRegistrationCreated(source);
            _metrics.RecordRegistrationCreated(source);
            _metrics.RecordRegistrationCreated(source);

            // Assert
            counter.WithLabels(source).Value.Should().Be(3);
        }

        /// <summary>
        /// Verifies that distinct "source" label values are tracked as independent time series,
        /// so incrementing one source does not affect the counter value of another source.
        /// </summary>
        [Fact]
        public void RecordRegistrationCreated_WhenCalledWithDifferentSources_TracksEachSourceIndependently()
        {
            // Arrange
            var selfServiceSource = $"self-service-{Guid.NewGuid()}";
            var adminSource = $"admin-{Guid.NewGuid()}";
            var counter = RegistrationsCreatedCounter();

            // Act
            _metrics.RecordRegistrationCreated(selfServiceSource);
            _metrics.RecordRegistrationCreated(adminSource);
            _metrics.RecordRegistrationCreated(adminSource);

            // Assert
            counter.WithLabels(selfServiceSource).Value.Should().Be(1);
            counter.WithLabels(adminSource).Value.Should().Be(2);
        }

        /// <summary>
        /// Verifies that recording a registration creation does not increment the unrelated
        /// registrations-cancelled counter, confirming the two metrics are fully isolated.
        /// </summary>
        [Fact]
        public void RecordRegistrationCreated_WhenCalled_DoesNotAffectRegistrationsCancelledCounter()
        {
            // Arrange
            var source = $"isolation-check-{Guid.NewGuid()}";
            var cancelledCounter = RegistrationsCancelledCounter();

            // Act
            _metrics.RecordRegistrationCreated(source);

            // Assert
            cancelledCounter.WithLabels(source).Value.Should().Be(0);
        }
    }

    /// <summary>
    /// Tests covering <see cref="ApplicationMetrics.RecordRegistrationCancelled"/>.
    /// </summary>
    public sealed class RecordRegistrationCancelledTests : ApplicationMetricsTests
    {
        /// <summary>
        /// Verifies that calling RecordRegistrationCancelled increments the registrations-cancelled
        /// counter for the given "source" label from zero to one.
        /// </summary>
        [Fact]
        public void RecordRegistrationCancelled_WhenCalledOnce_IncrementsCounterForSourceLabelByOne()
        {
            // Arrange
            var source = $"self-service-{Guid.NewGuid()}";
            var counter = RegistrationsCancelledCounter();

            // Act
            _metrics.RecordRegistrationCancelled(source);

            // Assert
            counter.WithLabels(source).Value.Should().Be(1);
        }

        /// <summary>
        /// Verifies that calling RecordRegistrationCancelled multiple times accumulates the counter
        /// value for the same "source" label instead of resetting it.
        /// </summary>
        [Fact]
        public void RecordRegistrationCancelled_WhenCalledMultipleTimes_AccumulatesCounterValue()
        {
            // Arrange
            var source = $"admin-{Guid.NewGuid()}";
            var counter = RegistrationsCancelledCounter();

            // Act
            _metrics.RecordRegistrationCancelled(source);
            _metrics.RecordRegistrationCancelled(source);

            // Assert
            counter.WithLabels(source).Value.Should().Be(2);
        }

        /// <summary>
        /// Verifies that recording a registration cancellation does not increment the unrelated
        /// registrations-created counter, confirming the two metrics are fully isolated.
        /// </summary>
        [Fact]
        public void RecordRegistrationCancelled_WhenCalled_DoesNotAffectRegistrationsCreatedCounter()
        {
            // Arrange
            var source = $"isolation-check-{Guid.NewGuid()}";
            var createdCounter = RegistrationsCreatedCounter();

            // Act
            _metrics.RecordRegistrationCancelled(source);

            // Assert
            createdCounter.WithLabels(source).Value.Should().Be(0);
        }
    }
}
