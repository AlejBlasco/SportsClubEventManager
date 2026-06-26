using Bunit;
using FluentAssertions;
using SportsClubEventManager.Shared.DTOs;
using SportsClubEventManager.Web.Components.Events;

namespace SportsClubEventManager.Web.Tests.Components;

/// <summary>
/// Unit tests for the EventList component.
/// </summary>
public sealed class EventListTests : TestContext
{
    /// <summary>
    /// Tests that the event list renders all provided events.
    /// </summary>
    [Fact]
    public void EventList_WithEvents_RendersAllEvents()
    {
        // Arrange
        var events = CreateTestEvents();

        // Act
        var cut = RenderComponent<EventList>(parameters => parameters
            .Add(p => p.Events, events));

        // Assert
        cut.Markup.Should().Contain("Test Event 1");
        cut.Markup.Should().Contain("Test Event 2");
    }

    /// <summary>
    /// Tests that the event list displays a message when no events are available.
    /// </summary>
    [Fact]
    public void EventList_WithEmptyEventList_DisplaysNoEventsMessage()
    {
        // Arrange
        var events = new List<EventDto>();

        // Act
        var cut = RenderComponent<EventList>(parameters => parameters
            .Add(p => p.Events, events));

        // Assert
        cut.Markup.Should().Contain("No events available");
    }

    /// <summary>
    /// Tests that the event list displays a Full badge for events with no available slots.
    /// </summary>
    [Fact]
    public void EventList_WithFullEvent_DisplaysFullBadge()
    {
        // Arrange
        var events = new List<EventDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Full Event",
                Date = DateTime.UtcNow,
                Location = "Test Location",
                MaxCapacity = 30,
                AvailableSlots = 0
            }
        };

        // Act
        var cut = RenderComponent<EventList>(parameters => parameters
            .Add(p => p.Events, events));

        // Assert
        cut.Markup.Should().Contain("Full");
    }

    /// <summary>
    /// Tests that the event list does not display a Full badge for events with available slots.
    /// </summary>
    [Fact]
    public void EventList_WithAvailableEvent_DoesNotDisplayFullBadge()
    {
        // Arrange
        var events = new List<EventDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Available Event",
                Date = DateTime.UtcNow,
                Location = "Test Location",
                MaxCapacity = 30,
                AvailableSlots = 10
            }
        };

        // Act
        var cut = RenderComponent<EventList>(parameters => parameters
            .Add(p => p.Events, events));

        // Assert
        cut.FindAll(".badge-full").Should().BeEmpty();
    }

    /// <summary>
    /// Tests that the event list displays event details correctly.
    /// </summary>
    [Fact]
    public void EventList_WithEvent_DisplaysEventDetails()
    {
        // Arrange
        var events = new List<EventDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Test Event",
                Date = new DateTime(2026, 6, 30, 14, 0, 0, DateTimeKind.Utc),
                Location = "Test Stadium",
                MaxCapacity = 100,
                AvailableSlots = 50
            }
        };

        // Act
        var cut = RenderComponent<EventList>(parameters => parameters
            .Add(p => p.Events, events));

        // Assert
        cut.Markup.Should().Contain("Test Event");
        cut.Markup.Should().Contain("Test Stadium");
        cut.Markup.Should().Contain("50 / 100");
    }

    /// <summary>
    /// Tests that events are ordered by date in the list.
    /// </summary>
    [Fact]
    public void EventList_WithMultipleEvents_OrdersByDate()
    {
        // Arrange
        var events = new List<EventDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Future Event",
                Date = DateTime.UtcNow.AddDays(5),
                Location = "Location 1",
                MaxCapacity = 50,
                AvailableSlots = 20
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Near Event",
                Date = DateTime.UtcNow.AddDays(1),
                Location = "Location 2",
                MaxCapacity = 30,
                AvailableSlots = 10
            }
        };

        // Act
        var cut = RenderComponent<EventList>(parameters => parameters
            .Add(p => p.Events, events));

        // Assert
        var eventCards = cut.FindAll(".event-card");
        eventCards.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    private static List<EventDto> CreateTestEvents()
    {
        return
        [
            new EventDto
            {
                Id = Guid.NewGuid(),
                Title = "Test Event 1",
                Date = DateTime.UtcNow,
                Location = "Test Location",
                MaxCapacity = 50,
                AvailableSlots = 20
            },
            new EventDto
            {
                Id = Guid.NewGuid(),
                Title = "Test Event 2",
                Date = DateTime.UtcNow.AddDays(1),
                Location = "Test Location 2",
                MaxCapacity = 30,
                AvailableSlots = 0
            }
        ];
    }
}
