using Bunit;
using FluentAssertions;
using SportsClubEventManager.Web.Components.Shared;

namespace SportsClubEventManager.Web.Tests.Components.Shared;

/// <summary>
/// Unit tests for the CapacityIndicator component.
/// </summary>
public sealed class CapacityIndicatorTests : TestContext
{
    /// <summary>
    /// Tests that the CapacityIndicator displays capacity information correctly.
    /// </summary>
    [Fact]
    public void CapacityIndicator_WhenRendered_DisplaysCapacityInformation()
    {
        // Arrange & Act
        var cut = RenderComponent<CapacityIndicator>(parameters => parameters
            .Add(p => p.MaxCapacity, 50)
            .Add(p => p.CurrentRegistrations, 30)
            .Add(p => p.AvailableSlots, 20)
            .Add(p => p.IsFullyBooked, false));

        // Assert
        var capacityValue = cut.Find(".capacity-value");
        capacityValue.TextContent.Should().Be("20 / 50");
    }

    /// <summary>
    /// Tests that the CapacityIndicator calculates percentage filled correctly.
    /// </summary>
    [Fact]
    public void CapacityIndicator_WhenRendered_CalculatesPercentageCorrectly()
    {
        // Arrange & Act
        var cut = RenderComponent<CapacityIndicator>(parameters => parameters
            .Add(p => p.MaxCapacity, 50)
            .Add(p => p.CurrentRegistrations, 25)
            .Add(p => p.AvailableSlots, 25)
            .Add(p => p.IsFullyBooked, false));

        // Assert
        var progressBar = cut.Find(".capacity-bar");
        var style = progressBar.GetAttribute("style");
        style.Should().Contain("width: 50%");
    }

    /// <summary>
    /// Tests that the CapacityIndicator shows fully booked badge when event is at capacity.
    /// </summary>
    [Fact]
    public void CapacityIndicator_WhenFullyBooked_DisplaysFullyBookedBadge()
    {
        // Arrange & Act
        var cut = RenderComponent<CapacityIndicator>(parameters => parameters
            .Add(p => p.MaxCapacity, 50)
            .Add(p => p.CurrentRegistrations, 50)
            .Add(p => p.AvailableSlots, 0)
            .Add(p => p.IsFullyBooked, true));

        // Assert
        var badge = cut.Find(".fully-booked-badge");
        badge.TextContent.Should().Be("Fully Booked");
    }

    /// <summary>
    /// Tests that the CapacityIndicator does not show fully booked badge when event has availability.
    /// </summary>
    [Fact]
    public void CapacityIndicator_WhenNotFullyBooked_DoesNotDisplayFullyBookedBadge()
    {
        // Arrange & Act
        var cut = RenderComponent<CapacityIndicator>(parameters => parameters
            .Add(p => p.MaxCapacity, 50)
            .Add(p => p.CurrentRegistrations, 30)
            .Add(p => p.AvailableSlots, 20)
            .Add(p => p.IsFullyBooked, false));

        // Assert
        var badges = cut.FindAll(".fully-booked-badge");
        badges.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that the CapacityIndicator includes proper ARIA attributes for accessibility.
    /// </summary>
    [Fact]
    public void CapacityIndicator_WhenRendered_IncludesAriaAttributes()
    {
        // Arrange & Act
        var cut = RenderComponent<CapacityIndicator>(parameters => parameters
            .Add(p => p.MaxCapacity, 50)
            .Add(p => p.CurrentRegistrations, 30)
            .Add(p => p.AvailableSlots, 20)
            .Add(p => p.IsFullyBooked, false));

        // Assert
        var progressBar = cut.Find(".capacity-bar");
        progressBar.GetAttribute("role").Should().Be("progressbar");
        progressBar.GetAttribute("aria-valuenow").Should().Be("30");
        progressBar.GetAttribute("aria-valuemin").Should().Be("0");
        progressBar.GetAttribute("aria-valuemax").Should().Be("50");
        progressBar.GetAttribute("aria-label").Should().Contain("30 out of 50");
    }

    /// <summary>
    /// Tests that the CapacityIndicator handles zero capacity gracefully.
    /// </summary>
    [Fact]
    public void CapacityIndicator_WhenMaxCapacityIsZero_DisplaysZeroPercentage()
    {
        // Arrange & Act
        var cut = RenderComponent<CapacityIndicator>(parameters => parameters
            .Add(p => p.MaxCapacity, 0)
            .Add(p => p.CurrentRegistrations, 0)
            .Add(p => p.AvailableSlots, 0)
            .Add(p => p.IsFullyBooked, false));

        // Assert
        var progressBar = cut.Find(".capacity-bar");
        var style = progressBar.GetAttribute("style");
        style.Should().Contain("width: 0%");
    }

    /// <summary>
    /// Tests that the CapacityIndicator displays 100 percent when fully booked.
    /// </summary>
    [Fact]
    public void CapacityIndicator_WhenFullyBooked_Displays100Percent()
    {
        // Arrange & Act
        var cut = RenderComponent<CapacityIndicator>(parameters => parameters
            .Add(p => p.MaxCapacity, 50)
            .Add(p => p.CurrentRegistrations, 50)
            .Add(p => p.AvailableSlots, 0)
            .Add(p => p.IsFullyBooked, true));

        // Assert
        var progressBar = cut.Find(".capacity-bar");
        var style = progressBar.GetAttribute("style");
        style.Should().Contain("width: 100%");
    }
}
