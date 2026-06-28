using FluentAssertions;
using SportsClubEventManager.Web.Services;

namespace SportsClubEventManager.Web.Tests.Services;

/// <summary>
/// Unit tests for the GuidProvider class.
/// </summary>
public sealed class GuidProviderTests
{
    /// <summary>
    /// Tests that NewGuid returns a valid non-empty Guid.
    /// </summary>
    [Fact]
    public void NewGuid_WhenCalled_ReturnsNonEmptyGuid()
    {
        // Arrange
        var provider = new GuidProvider();

        // Act
        var result = provider.NewGuid();

        // Assert
        result.Should().NotBeEmpty();
    }

    /// <summary>
    /// Tests that NewGuid generates unique values on consecutive calls.
    /// </summary>
    [Fact]
    public void NewGuid_WhenCalledMultipleTimes_ReturnsUniqueGuids()
    {
        // Arrange
        var provider = new GuidProvider();

        // Act
        var guid1 = provider.NewGuid();
        var guid2 = provider.NewGuid();
        var guid3 = provider.NewGuid();

        // Assert
        guid1.Should().NotBe(guid2);
        guid2.Should().NotBe(guid3);
        guid1.Should().NotBe(guid3);
    }

    /// <summary>
    /// Tests that NewGuid implements the IGuidProvider interface correctly.
    /// </summary>
    [Fact]
    public void NewGuid_ImplementsInterface_CanBeUsedPolymorphically()
    {
        // Arrange
        IGuidProvider provider = new GuidProvider();

        // Act
        var result = provider.NewGuid();

        // Assert
        result.Should().NotBeEmpty();
    }
}
