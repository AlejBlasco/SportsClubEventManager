using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SportsClubEventManager.Shared.DTOs;
using SportsClubEventManager.Web.Components.Pages;
using SportsClubEventManager.Web.Services;

namespace SportsClubEventManager.Web.Tests.Components;

/// <summary>
/// Unit tests for the EventDetails page component.
/// </summary>
public sealed class EventDetailsPageTests : TestContext
{
    private readonly IEventService _eventService;
    private readonly IRegistrationService _registrationService;

    /// <summary>
    /// Initializes the test with mocked dependencies.
    /// </summary>
    public EventDetailsPageTests()
    {
        _eventService = Substitute.For<IEventService>();
        _registrationService = Substitute.For<IRegistrationService>();
        _registrationService.GetMyRegistrationsAsync(Arg.Any<CancellationToken>())
            .Returns([]);
        Services.AddSingleton(_eventService);
        Services.AddSingleton(_registrationService);
    }

    /// <summary>
    /// Tests that the EventDetails page displays loading spinner while loading.
    /// </summary>
    [Fact]
    public void EventDetailsPage_WhenLoading_DisplaysLoadingSpinner()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var tcs = new TaskCompletionSource<EventDetailDto?>();
        _eventService.GetEventByIdAsync(eventId, Arg.Any<CancellationToken>()).Returns(tcs.Task);

        // Act
        var cut = RenderComponent<EventDetails>(parameters => parameters
            .Add(p => p.Id, eventId));

        // Assert
        var loadingMessage = cut.Find(".loading-message");
        loadingMessage.TextContent.Should().Contain("Loading");
    }

    /// <summary>
    /// Tests that the EventDetails page displays event information when loaded successfully.
    /// </summary>
    [Fact]
    public void EventDetailsPage_WhenEventLoaded_DisplaysEventInformation()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventDetail = CreateTestEvent(eventId);
        _eventService.GetEventByIdAsync(eventId, Arg.Any<CancellationToken>()).Returns(eventDetail);

        // Act
        var cut = RenderComponent<EventDetails>(parameters => parameters
            .Add(p => p.Id, eventId));

        // Assert
        var title = cut.Find(".event-title");
        title.TextContent.Should().Be(eventDetail.Title);

        var description = cut.Find(".event-description");
        description.TextContent.Should().Be(eventDetail.Description);
    }

    /// <summary>
    /// Tests that the EventDetails page displays not found component when event does not exist.
    /// </summary>
    [Fact]
    public void EventDetailsPage_WhenEventNotFound_DisplaysNotFoundPage()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        _eventService.GetEventByIdAsync(eventId, Arg.Any<CancellationToken>()).Returns((EventDetailDto?)null);

        // Act
        var cut = RenderComponent<EventDetails>(parameters => parameters
            .Add(p => p.Id, eventId));

        // Assert
        var notFoundHeading = cut.Find("h2");
        notFoundHeading.TextContent.Should().Be("Event Not Found");
    }

    /// <summary>
    /// Tests that the EventDetails page displays error message when API call fails.
    /// </summary>
    [Fact]
    public void EventDetailsPage_WhenApiCallFails_DisplaysErrorMessage()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        _eventService.GetEventByIdAsync(eventId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<EventDetailDto?>(new HttpRequestException("API unavailable")));

        // Act
        var cut = RenderComponent<EventDetails>(parameters => parameters
            .Add(p => p.Id, eventId));

        // Assert
        var errorMessage = cut.Find(".error-text");
        errorMessage.TextContent.Should().Contain("Unable to load event details");
    }

    /// <summary>
    /// Tests that the EventDetails page displays fully booked badge for fully booked events.
    /// </summary>
    [Fact]
    public void EventDetailsPage_WhenEventFullyBooked_DisplaysFullyBookedBadge()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventDetail = CreateTestEvent(eventId, isFullyBooked: true);
        _eventService.GetEventByIdAsync(eventId, Arg.Any<CancellationToken>()).Returns(eventDetail);

        // Act
        var cut = RenderComponent<EventDetails>(parameters => parameters
            .Add(p => p.Id, eventId));

        // Assert
        var badge = cut.Find(".badge-fully-booked");
        badge.TextContent.Should().Be("Fully Booked");
    }

    /// <summary>
    /// Tests that the EventDetails page does not display fully booked badge when event has availability.
    /// </summary>
    [Fact]
    public void EventDetailsPage_WhenEventHasAvailability_DoesNotDisplayFullyBookedBadge()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventDetail = CreateTestEvent(eventId, isFullyBooked: false);
        _eventService.GetEventByIdAsync(eventId, Arg.Any<CancellationToken>()).Returns(eventDetail);

        // Act
        var cut = RenderComponent<EventDetails>(parameters => parameters
            .Add(p => p.Id, eventId));

        // Assert
        var badges = cut.FindAll(".badge-fully-booked");
        badges.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that the EventDetails page displays capacity indicator.
    /// </summary>
    [Fact]
    public void EventDetailsPage_WhenEventLoaded_DisplaysCapacityIndicator()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventDetail = CreateTestEvent(eventId);
        _eventService.GetEventByIdAsync(eventId, Arg.Any<CancellationToken>()).Returns(eventDetail);

        // Act
        var cut = RenderComponent<EventDetails>(parameters => parameters
            .Add(p => p.Id, eventId));

        // Assert
        var capacityIndicator = cut.Find(".capacity-indicator");
        capacityIndicator.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that the EventDetails page displays back navigation link.
    /// </summary>
    [Fact]
    public void EventDetailsPage_WhenEventLoaded_DisplaysBackNavigationLink()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventDetail = CreateTestEvent(eventId);
        _eventService.GetEventByIdAsync(eventId, Arg.Any<CancellationToken>()).Returns(eventDetail);

        // Act
        var cut = RenderComponent<EventDetails>(parameters => parameters
            .Add(p => p.Id, eventId));

        // Assert
        var backLink = cut.Find(".back-link");
        backLink.GetAttribute("href").Should().Be("/events");
        backLink.TextContent.Should().Contain("Back to Events");
    }

    /// <summary>
    /// Tests that the EventDetails page can retry loading after an error.
    /// </summary>
    [Fact]
    public void EventDetailsPage_WhenErrorOccurs_AllowsRetry()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        _eventService.GetEventByIdAsync(eventId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<EventDetailDto?>(new HttpRequestException("API unavailable")));

        var cut = RenderComponent<EventDetails>(parameters => parameters
            .Add(p => p.Id, eventId));

        var eventDetail = CreateTestEvent(eventId);
        _eventService.GetEventByIdAsync(eventId, Arg.Any<CancellationToken>()).Returns(eventDetail);

        // Act
        var retryButton = cut.Find("button");
        retryButton.Click();

        // Assert
        var title = cut.Find(".event-title");
        title.TextContent.Should().Be(eventDetail.Title);
    }

    /// <summary>
    /// Tests that the EventDetails page calls event service with correct ID.
    /// </summary>
    [Fact]
    public async Task EventDetailsPage_WhenRendered_CallsEventServiceWithCorrectId()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventDetail = CreateTestEvent(eventId);
        _eventService.GetEventByIdAsync(eventId, Arg.Any<CancellationToken>()).Returns(eventDetail);

        // Act
        var cut = RenderComponent<EventDetails>(parameters => parameters
            .Add(p => p.Id, eventId));

        // Assert
        await _eventService.Received(1).GetEventByIdAsync(eventId, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Tests that the EventDetails page displays event date in readable format.
    /// </summary>
    [Fact]
    public void EventDetailsPage_WhenEventLoaded_DisplaysDateInReadableFormat()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventDetail = CreateTestEvent(eventId);
        _eventService.GetEventByIdAsync(eventId, Arg.Any<CancellationToken>()).Returns(eventDetail);

        // Act
        var cut = RenderComponent<EventDetails>(parameters => parameters
            .Add(p => p.Id, eventId));

        // Assert
        var infoList = cut.Find(".event-info-list");
        infoList.TextContent.Should().Contain(eventDetail.Date.ToLocalTime().ToString("MMMM"));
    }

    /// <summary>
    /// Tests that the EventDetails page handles event with no description.
    /// </summary>
    [Fact]
    public void EventDetailsPage_WhenEventHasNoDescription_DoesNotDisplayDescriptionSection()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventDetail = CreateTestEvent(eventId, description: null);
        _eventService.GetEventByIdAsync(eventId, Arg.Any<CancellationToken>()).Returns(eventDetail);

        // Act
        var cut = RenderComponent<EventDetails>(parameters => parameters
            .Add(p => p.Id, eventId));

        // Assert
        var descriptionElements = cut.FindAll(".event-description");
        descriptionElements.Should().BeEmpty();
    }

    private static EventDetailDto CreateTestEvent(Guid id, bool isFullyBooked = false, string? description = "Test Event Description")
    {
        return new EventDetailDto
        {
            Id = id,
            Title = "Test Event",
            Description = description,
            Date = DateTime.UtcNow.AddDays(7),
            Location = "Test Location",
            MaxCapacity = 50,
            CurrentRegistrations = isFullyBooked ? 50 : 30,
            AvailableSlots = isFullyBooked ? 0 : 20,
            IsFullyBooked = isFullyBooked
        };
    }
}
