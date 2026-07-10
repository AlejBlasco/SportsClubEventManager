using FluentAssertions;
using SportsClubEventManager.Web.Services;

namespace SportsClubEventManager.Web.Tests.Services;

/// <summary>
/// Unit tests for the CorrelationIdProvider class, verifying it generates a single, stable
/// correlation id per instance (per Blazor Server circuit, since it is registered Scoped), rather
/// than a new id on every read.
/// </summary>
public sealed class CorrelationIdProviderTests
{
    /// <summary>
    /// Verifies that reading CorrelationId multiple times on the same instance always returns the
    /// same value, i.e. it is generated once in the constructor, not per-call.
    /// </summary>
    [Fact]
    public void CorrelationId_WhenReadMultipleTimesOnSameInstance_ReturnsSameValue()
    {
        // Arrange
        var provider = new CorrelationIdProvider();

        // Act
        var firstRead = provider.CorrelationId;
        var secondRead = provider.CorrelationId;
        var thirdRead = provider.CorrelationId;

        // Assert
        firstRead.Should().Be(secondRead);
        secondRead.Should().Be(thirdRead);
    }

    /// <summary>
    /// Verifies that two different instances of CorrelationIdProvider (representing two different
    /// DI scopes/circuits) receive different correlation ids.
    /// </summary>
    [Fact]
    public void CorrelationId_WhenComparingTwoInstances_ReturnsDifferentValues()
    {
        // Arrange
        var firstProvider = new CorrelationIdProvider();
        var secondProvider = new CorrelationIdProvider();

        // Act
        var firstId = firstProvider.CorrelationId;
        var secondId = secondProvider.CorrelationId;

        // Assert
        firstId.Should().NotBe(secondId);
    }

    /// <summary>
    /// Verifies that the generated correlation id is a valid, non-empty GUID, matching the
    /// contract the design relies on for propagation as the X-Correlation-Id header value.
    /// </summary>
    [Fact]
    public void CorrelationId_WhenGenerated_IsAValidNonEmptyGuid()
    {
        // Arrange
        var provider = new CorrelationIdProvider();

        // Act
        var correlationId = provider.CorrelationId;

        // Assert
        correlationId.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(correlationId, out _).Should().BeTrue();
    }
}
