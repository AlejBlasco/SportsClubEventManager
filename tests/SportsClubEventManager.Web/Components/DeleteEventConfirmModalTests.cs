using Bunit;
using FluentAssertions;
using SportsClubEventManager.Shared.DTOs;
using SportsClubEventManager.Web.Components.Admin;
using Xunit;

namespace SportsClubEventManager.Web.Tests.Components;

/// <summary>
/// Tests for DeleteEventConfirmModal component to verify delete confirmation UX and registration count display.
/// </summary>
public class DeleteEventConfirmModalTests : TestContext
{
    /// <summary>
    /// Verifies that delete confirmation modal renders with event title.
    /// </summary>
    [Fact]
    public void Render_WhenEventDetailsProvided_DisplaysEventTitle()
    {
        // Arrange
        var eventItem = new EventAdminListDto
        {
            Id = Guid.NewGuid(),
            Title = "Basketball Tournament",
            Date = DateTime.UtcNow.AddDays(7),
            Location = "Sports Hall A",
            MaxCapacity = 100,
            RegistrationCount = 0,
            IsPastEvent = false
        };

        // Act
        var cut = RenderComponent<DeleteEventConfirmModal>(parameters => parameters
            .Add(p => p.EventItem, eventItem));

        // Assert
        cut.Should().NotBeNull();
        cut.Markup.Should().Contain("Basketball Tournament");
    }

    /// <summary>
    /// Verifies that modal displays registration count (RISK-7 mitigation).
    /// </summary>
    [Fact]
    public void Render_WhenEventHasRegistrations_DisplaysRegistrationCount()
    {
        // Arrange
        var eventItem = new EventAdminListDto
        {
            Id = Guid.NewGuid(),
            Title = "Test Event",
            Date = DateTime.UtcNow.AddDays(7),
            Location = "Test Location",
            MaxCapacity = 100,
            RegistrationCount = 25,
            IsPastEvent = false
        };

        // Act
        var cut = RenderComponent<DeleteEventConfirmModal>(parameters => parameters
            .Add(p => p.EventItem, eventItem));

        // Assert
        cut.Should().NotBeNull();
        cut.Markup.Should().Contain("25");
    }

    /// <summary>
    /// Verifies that modal correctly displays zero registrations.
    /// </summary>
    [Fact]
    public void Render_WhenEventHasNoRegistrations_DisplaysZeroRegistrations()
    {
        // Arrange
        var eventItem = new EventAdminListDto
        {
            Id = Guid.NewGuid(),
            Title = "Test Event",
            Date = DateTime.UtcNow.AddDays(7),
            Location = "Test Location",
            MaxCapacity = 100,
            RegistrationCount = 0,
            IsPastEvent = false
        };

        // Act
        var cut = RenderComponent<DeleteEventConfirmModal>(parameters => parameters
            .Add(p => p.EventItem, eventItem));

        // Assert
        cut.Should().NotBeNull();
        cut.Markup.Should().Contain("0");
    }

    /// <summary>
    /// Verifies that modal displays large registration count.
    /// </summary>
    [Fact]
    public void Render_WhenEventHasLargeRegistrationCount_DisplaysLargeNumber()
    {
        // Arrange
        var eventItem = new EventAdminListDto
        {
            Id = Guid.NewGuid(),
            Title = "Test Event",
            Date = DateTime.UtcNow.AddDays(7),
            Location = "Test Location",
            MaxCapacity = 1000,
            RegistrationCount = 500,
            IsPastEvent = false
        };

        // Act
        var cut = RenderComponent<DeleteEventConfirmModal>(parameters => parameters
            .Add(p => p.EventItem, eventItem));

        // Assert
        cut.Should().NotBeNull();
        cut.Markup.Should().Contain("500");
    }
}
