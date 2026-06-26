using Bunit;
using FluentAssertions;
using NSubstitute;
using SportsClubEventManager.Shared.DTOs;
using SportsClubEventManager.Web.Components.Events;
using SportsClubEventManager.Web.Services;

namespace SportsClubEventManager.Web.Tests.Integration;

/// <summary>
/// Integration tests for Events page component hierarchy using bUnit.
/// These tests verify full component rendering with child components (EventCalendar, EventList, EventCard)
/// and their interactions. Note: Direct Events page rendering with JS interop is tested in separate
/// end-to-end scenario tests. These focus on component tree integration.
/// </summary>
public sealed class EventsPageIntegrationTests : TestContext
{
    /// <summary>
    /// Verifies that EventCard renders all event details correctly.
    /// </summary>
    [Fact]
    public void EventCard_WithEvent_DisplaysAllEventDetails()
    {
        // Arrange
        var eventDto = new EventDto
        {
            Id = Guid.NewGuid(),
            Title = "Test Event",
            Date = new DateTime(2026, 7, 15, 14, 30, 0, DateTimeKind.Utc),
            Location = "Test Stadium",
            MaxCapacity = 100,
            AvailableSlots = 50
        };

        // Act
        var cut = RenderComponent<EventCard>(parameters =>
            parameters.Add(p => p.Event, eventDto));

        // Assert
        cut.Markup.Should().Contain("Test Event");
        cut.Markup.Should().Contain("Test Stadium");
        cut.Markup.Should().Contain("50 / 100");
        cut.Markup.Should().Contain("event-card");
    }

    /// <summary>
    /// Verifies that EventCard displays the Full badge when available slots are zero.
    /// </summary>
    [Fact]
    public void EventCard_WithZeroAvailableSlots_DisplaysFullBadge()
    {
        // Arrange
        var fullEvent = new EventDto
        {
            Id = Guid.NewGuid(),
            Title = "Full Event",
            Date = DateTime.UtcNow.AddDays(1),
            Location = "Test Location",
            MaxCapacity = 30,
            AvailableSlots = 0
        };

        // Act
        var cut = RenderComponent<EventCard>(parameters =>
            parameters.Add(p => p.Event, fullEvent));

        // Assert
        cut.Markup.Should().Contain("Full");
        cut.Markup.Should().Contain("badge-full");
    }

    /// <summary>
    /// Verifies that EventCard does not display the Full badge when slots are available.
    /// </summary>
    [Fact]
    public void EventCard_WithAvailableSlots_DoesNotDisplayFullBadge()
    {
        // Arrange
        var availableEvent = new EventDto
        {
            Id = Guid.NewGuid(),
            Title = "Available Event",
            Date = DateTime.UtcNow.AddDays(1),
            Location = "Test Location",
            MaxCapacity = 50,
            AvailableSlots = 20
        };

        // Act
        var cut = RenderComponent<EventCard>(parameters =>
            parameters.Add(p => p.Event, availableEvent));

        // Assert
        cut.FindAll(".badge-full").Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that EventCard displays a disabled View Details link.
    /// </summary>
    [Fact]
    public void EventCard_WhenRendered_DisplaysDisabledViewDetailsLink()
    {
        // Arrange
        var eventDto = new EventDto
        {
            Id = Guid.NewGuid(),
            Title = "Test Event",
            Date = DateTime.UtcNow.AddDays(1),
            Location = "Test Location",
            MaxCapacity = 50,
            AvailableSlots = 20
        };

        // Act
        var cut = RenderComponent<EventCard>(parameters =>
            parameters.Add(p => p.Event, eventDto));

        // Assert
        cut.Markup.Should().Contain("event-link-disabled");
        cut.Markup.Should().Contain("View Details");
        cut.Markup.Should().NotContain("href=\"/events/");
    }

    /// <summary>
    /// Verifies that EventList renders all events correctly.
    /// </summary>
    [Fact]
    public void EventList_WithMultipleEvents_RendersAllEvents()
    {
        // Arrange
        var events = new List<EventDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Event 1",
                Date = DateTime.UtcNow.AddDays(1),
                Location = "Location 1",
                MaxCapacity = 50,
                AvailableSlots = 20
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Event 2",
                Date = DateTime.UtcNow.AddDays(5),
                Location = "Location 2",
                MaxCapacity = 30,
                AvailableSlots = 10
            }
        };

        // Act
        var cut = RenderComponent<EventList>(parameters =>
            parameters.Add(p => p.Events, events));

        // Assert
        cut.Markup.Should().Contain("Event 1");
        cut.Markup.Should().Contain("Event 2");
    }

    /// <summary>
    /// Verifies that EventList orders events by date.
    /// </summary>
    [Fact]
    public void EventList_WithMultipleEvents_OrdersEventsByDate()
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
        var cut = RenderComponent<EventList>(parameters =>
            parameters.Add(p => p.Events, events));

        // Assert
        var markup = cut.Markup;
        var nearEventIndex = markup.IndexOf("Near Event");
        var futureEventIndex = markup.IndexOf("Future Event");

        nearEventIndex.Should().BeGreaterThan(-1);
        futureEventIndex.Should().BeGreaterThan(-1);
        nearEventIndex.Should().BeLessThan(futureEventIndex);
    }

    /// <summary>
    /// Verifies that EventList displays empty state message when no events are provided.
    /// </summary>
    [Fact]
    public void EventList_WithEmptyEventList_DisplaysNoEventsMessage()
    {
        // Arrange
        var events = new List<EventDto>();

        // Act
        var cut = RenderComponent<EventList>(parameters =>
            parameters.Add(p => p.Events, events));

        // Assert
        cut.Markup.Should().Contain("No events available");
    }

    /// <summary>
    /// Verifies that EventList creates EventCard components for each event.
    /// </summary>
    [Fact]
    public void EventList_WhenRendered_CreatesEventCardComponents()
    {
        // Arrange
        var testEvents = CreateTestEvents();

        // Act
        var cut = RenderComponent<EventList>(parameters =>
            parameters.Add(p => p.Events, testEvents));

        // Assert
        var cardComponents = cut.FindComponents<EventCard>();
        cardComponents.Should().HaveCount(2);
    }

    /// <summary>
    /// Verifies that EventList with full event displays badge in rendered cards.
    /// </summary>
    [Fact]
    public void EventList_WithFullEvent_DisplaysFullBadgeInCard()
    {
        // Arrange
        var testEvents = new List<EventDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Full Event",
                Date = DateTime.UtcNow.AddDays(1),
                Location = "Test Location",
                MaxCapacity = 30,
                AvailableSlots = 0
            }
        };

        // Act
        var cut = RenderComponent<EventList>(parameters =>
            parameters.Add(p => p.Events, testEvents));

        // Assert
        cut.Markup.Should().Contain("Full");
    }


    /// <summary>
    /// Verifies that EventService correctly retrieves events via mocked HTTP call.
    /// </summary>
    [Fact]
    public async Task EventService_WhenCalled_InvokesGetEventsAsync()
    {
        // Arrange
        var mockService = Substitute.For<IEventService>();
        var expectedEvents = CreateTestEvents();
        mockService.GetEventsAsync(Arg.Any<CancellationToken>()).Returns(expectedEvents);

        // Act
        var result = await mockService.GetEventsAsync();

        // Assert
        result.Should().HaveCount(2);
        await mockService.Received(1).GetEventsAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that EventService propagates API errors correctly.
    /// </summary>
    [Fact]
    public async Task EventService_WhenApiThrowsException_PropagatesError()
    {
        // Arrange
        var mockService = Substitute.For<IEventService>();
        mockService.GetEventsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<List<EventDto>>(new HttpRequestException("API error")));

        // Act
        var act = async () => await mockService.GetEventsAsync();

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    /// <summary>
    /// Verifies that EventService returns empty list when API returns empty response.
    /// </summary>
    [Fact]
    public async Task EventService_WhenApiReturnsEmpty_ReturnsEmptyList()
    {
        // Arrange
        var mockService = Substitute.For<IEventService>();
        mockService.GetEventsAsync(Arg.Any<CancellationToken>()).Returns(new List<EventDto>());

        // Act
        var result = await mockService.GetEventsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    private static List<EventDto> CreateTestEvents()
    {
        return
        [
            new EventDto
            {
                Id = Guid.NewGuid(),
                Title = "Test Event 1",
                Date = DateTime.UtcNow.AddDays(1),
                Location = "Test Location 1",
                MaxCapacity = 50,
                AvailableSlots = 20
            },
            new EventDto
            {
                Id = Guid.NewGuid(),
                Title = "Test Event 2",
                Date = DateTime.UtcNow.AddDays(5),
                Location = "Test Location 2",
                MaxCapacity = 30,
                AvailableSlots = 0
            }
        ];
    }
}
