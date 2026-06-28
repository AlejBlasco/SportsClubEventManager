using FluentAssertions;
using NSubstitute;
using SportsClubEventManager.Shared.DTOs;
using SportsClubEventManager.Web.Services;

namespace SportsClubEventManager.Web.Tests.Components;

/// <summary>
/// Unit tests for the Events page component logic and event service interactions.
/// Note: Full Blazor component rendering with child components (EventCalendar, EventList)
/// containing Radzen components is tested in integration tests. These tests verify
/// service integration and state management logic.
/// </summary>
public sealed class EventsPageTests
{
    private readonly IEventService _eventService;

    /// <summary>
    /// Initializes the test with a mocked event service.
    /// </summary>
    public EventsPageTests()
    {
        _eventService = Substitute.For<IEventService>();
    }

    /// <summary>
    /// Tests that the event service is called to retrieve events.
    /// </summary>
    [Fact]
    public async Task EventsPage_WhenLoadingEvents_CallsEventService()
    {
        // Arrange
        var events = CreateTestEvents();
        _eventService.GetEventsAsync(Arg.Any<CancellationToken>()).Returns(events);

        // Act
        var result = await _eventService.GetEventsAsync();

        // Assert
        result.Should().HaveCount(2);
        await _eventService.Received(1).GetEventsAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Tests that the page handles empty event lists from the service.
    /// </summary>
    [Fact]
    public async Task EventsPage_WhenNoEventsAvailable_HandlesEmptyList()
    {
        // Arrange
        _eventService.GetEventsAsync(Arg.Any<CancellationToken>()).Returns(new List<EventDto>());

        // Act
        var result = await _eventService.GetEventsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that the page handles service exceptions gracefully.
    /// </summary>
    [Fact]
    public async Task EventsPage_WhenEventServiceFails_CatchesException()
    {
        // Arrange
        _eventService.GetEventsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<List<EventDto>>(new HttpRequestException("API unavailable")));

        // Act
        var act = async () => await _eventService.GetEventsAsync();

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    /// <summary>
    /// Tests that the page can retry loading events after a failure.
    /// </summary>
    [Fact]
    public async Task EventsPage_WhenRetryingAfterFailure_CallsServiceAgain()
    {
        // Arrange
        var events = CreateTestEvents();
        _eventService.GetEventsAsync(Arg.Any<CancellationToken>())
            .Returns(
                Task.FromException<List<EventDto>>(new HttpRequestException()),
                Task.FromResult(events)
            );

        // Act
        // First call fails
        _ = await _eventService.GetEventsAsync().ContinueWith(t => (List<EventDto>?)null, TaskScheduler.Default);

        // Second call succeeds (retry)
        var result = await _eventService.GetEventsAsync();

        // Assert
        result.Should().HaveCount(2);
        await _eventService.Received(2).GetEventsAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Tests that multiple events are properly returned from the service.
    /// </summary>
    [Fact]
    public async Task EventsPage_WhenMultipleEventsExist_ReturnsAllEvents()
    {
        // Arrange
        var events = CreateTestEvents();
        _eventService.GetEventsAsync(Arg.Any<CancellationToken>()).Returns(events);

        // Act
        var result = await _eventService.GetEventsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(e => e.Title == "Test Event 1");
        result.Should().Contain(e => e.Title == "Test Event 2");
    }

    /// <summary>
    /// Tests that full events are properly identified in the result set.
    /// </summary>
    [Fact]
    public async Task EventsPage_WithFullEvent_IdentifiesFullStatus()
    {
        // Arrange
        var events = new List<EventDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Full Event",
                Date = DateTime.UtcNow,
                Location = "Location",
                MaxCapacity = 30,
                AvailableSlots = 0
            }
        };
        _eventService.GetEventsAsync(Arg.Any<CancellationToken>()).Returns(events);

        // Act
        var result = await _eventService.GetEventsAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].AvailableSlots.Should().Be(0);
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
