using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using SportsClubEventManager.Domain.Enums;
using SportsClubEventManager.Shared.DTOs;
using SportsClubEventManager.Web.Services;

namespace SportsClubEventManager.Web.Tests.Services;

/// <summary>
/// Unit tests for the EventService class.
/// </summary>
public sealed class EventServiceTests
{
    /// <summary>
    /// Tests that GetEventsAsync returns events when the API responds successfully.
    /// </summary>
    [Fact]
    public async Task GetEventsAsync_WhenApiReturnsEvents_ReturnsEventList()
    {
        // Arrange
        var expectedEvents = new List<EventDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Test Event 1",
                Date = DateTime.UtcNow,
                Location = "Test Location",
                MaxCapacity = 50,
                AvailableSlots = 20
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Test Event 2",
                Date = DateTime.UtcNow.AddDays(1),
                Location = "Test Location 2",
                MaxCapacity = 30,
                AvailableSlots = 0
            }
        };

        var httpClient = CreateHttpClientWithResponse(HttpStatusCode.OK, expectedEvents);
        var service = new EventService(httpClient);

        // Act
        var result = await service.GetEventsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(expectedEvents);
    }

    /// <summary>
    /// Tests that GetEventsAsync returns an empty list when the API returns no events.
    /// </summary>
    [Fact]
    public async Task GetEventsAsync_WhenApiReturnsEmptyList_ReturnsEmptyList()
    {
        // Arrange
        var httpClient = CreateHttpClientWithResponse(HttpStatusCode.OK, new List<EventDto>());
        var service = new EventService(httpClient);

        // Act
        var result = await service.GetEventsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that GetEventsAsync throws HttpRequestException when the API returns an error status code.
    /// </summary>
    [Fact]
    public async Task GetEventsAsync_WhenApiReturnsError_ThrowsHttpRequestException()
    {
        // Arrange
        var httpClient = CreateHttpClientWithResponse(HttpStatusCode.InternalServerError, (List<EventDto>?)null);
        var service = new EventService(httpClient);

        // Act
        var act = async () => await service.GetEventsAsync();

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    /// <summary>
    /// Tests that GetEventsAsync returns an empty list when the API returns a valid
    /// response with null content (which ReadFromJsonAsync interprets as null/empty).
    /// </summary>
    [Fact]
    public async Task GetEventsAsync_WhenApiReturnsEmptyContent_ReturnsEmptyList()
    {
        // Arrange
        var httpClient = CreateHttpClientWithResponse(HttpStatusCode.OK, new List<EventDto>());
        var service = new EventService(httpClient);

        // Act
        var result = await service.GetEventsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that GetEventsAsync respects cancellation token.
    /// </summary>
    [Fact]
    public async Task GetEventsAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var httpClient = CreateHttpClientWithDelay();
        var service = new EventService(httpClient);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = async () => await service.GetEventsAsync(cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    /// <summary>
    /// Tests that GetEventByIdAsync returns event details when the API responds successfully.
    /// </summary>
    [Fact]
    public async Task GetEventByIdAsync_WhenApiReturnsEvent_ReturnsEventDetail()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var expectedEvent = new EventDetailDto
        {
            Id = eventId,
            Title = "Test Event",
            Description = "Test Description",
            Date = DateTime.UtcNow,
            Location = "Test Location",
            MaxCapacity = 50,
            CurrentRegistrations = 30,
            AvailableSlots = 20,
            IsFullyBooked = false
        };

        var httpClient = CreateHttpClientWithDetailResponse(HttpStatusCode.OK, expectedEvent);
        var service = new EventService(httpClient);

        // Act
        var result = await service.GetEventByIdAsync(eventId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedEvent);
    }

    /// <summary>
    /// Tests that GetEventByIdAsync returns null when the API returns 404 Not Found.
    /// </summary>
    [Fact]
    public async Task GetEventByIdAsync_WhenApiReturns404_ReturnsNull()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var httpClient = CreateHttpClientWithDetailResponse(HttpStatusCode.NotFound, null);
        var service = new EventService(httpClient);

        // Act
        var result = await service.GetEventByIdAsync(eventId);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Tests that GetEventByIdAsync throws HttpRequestException when the API returns 500 Server Error.
    /// </summary>
    [Fact]
    public async Task GetEventByIdAsync_WhenApiReturns500_ThrowsHttpRequestException()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var httpClient = CreateHttpClientWithDetailResponse(HttpStatusCode.InternalServerError, null);
        var service = new EventService(httpClient);

        // Act
        var act = async () => await service.GetEventByIdAsync(eventId);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    /// <summary>
    /// Tests that GetEventByIdAsync respects cancellation token.
    /// </summary>
    [Fact]
    public async Task GetEventByIdAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var httpClient = CreateHttpClientWithDelay();
        var service = new EventService(httpClient);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = async () => await service.GetEventByIdAsync(eventId, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    /// <summary>
    /// Tests that GetEventByIdAsync returns event when fully booked.
    /// </summary>
    [Fact]
    public async Task GetEventByIdAsync_WhenEventFullyBooked_ReturnsEventWithIsFullyBookedTrue()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var expectedEvent = new EventDetailDto
        {
            Id = eventId,
            Title = "Fully Booked Event",
            Description = "This event is at capacity",
            Date = DateTime.UtcNow,
            Location = "Test Location",
            MaxCapacity = 50,
            CurrentRegistrations = 50,
            AvailableSlots = 0,
            IsFullyBooked = true
        };

        var httpClient = CreateHttpClientWithDetailResponse(HttpStatusCode.OK, expectedEvent);
        var service = new EventService(httpClient);

        // Act
        var result = await service.GetEventByIdAsync(eventId);

        // Assert
        result.Should().NotBeNull();
        result!.IsFullyBooked.Should().BeTrue();
        result.AvailableSlots.Should().Be(0);
    }

    private static HttpClient CreateHttpClientWithResponse(HttpStatusCode statusCode, List<EventDto>? events)
    {
        var handler = new TestHttpMessageHandler(statusCode, events);
        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost:7001")
        };
    }

    private static HttpClient CreateHttpClientWithDelay()
    {
        var handler = new DelayHttpMessageHandler();
        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost:7001")
        };
    }

    private static HttpClient CreateHttpClientWithDetailResponse(HttpStatusCode statusCode, EventDetailDto? eventDetail)
    {
        var handler = new TestHttpMessageHandlerForDetail(statusCode, eventDetail);
        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost:7001")
        };
    }

    private sealed class TestHttpMessageHandler(HttpStatusCode statusCode, List<EventDto>? events) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(statusCode);
            if (statusCode == HttpStatusCode.OK && events is not null)
            {
                response.Content = JsonContent.Create(events);
            }
            return Task.FromResult(response);
        }
    }

    private sealed class TestHttpMessageHandlerForDetail(HttpStatusCode statusCode, EventDetailDto? eventDetail) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(statusCode);
            if (statusCode == HttpStatusCode.OK && eventDetail is not null)
            {
                response.Content = JsonContent.Create(eventDetail);
            }
            return Task.FromResult(response);
        }
    }

    private sealed class DelayHttpMessageHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    /// <summary>
    /// Tests that RegisterForEventAsync returns registration details when the API responds successfully.
    /// </summary>
    [Fact]
    public async Task RegisterForEventAsync_WhenApiReturns201_ReturnsRegistrationCreatedDto()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var registrationCreated = new RegistrationCreatedDto
        {
            RegistrationId = Guid.NewGuid(),
            EventId = eventId,
            UserId = userId,
            RegisteredAt = DateTime.UtcNow,
            Status = RegistrationStatus.Registered,
            Event = new EventDetailDto
            {
                Id = eventId,
                Title = "Test Event",
                Date = DateTime.UtcNow,
                Location = "Test Location",
                MaxCapacity = 50,
                CurrentRegistrations = 31,
                AvailableSlots = 19,
                IsFullyBooked = false
            }
        };

        var httpClient = CreateHttpClientWithRegistrationResponse(HttpStatusCode.Created, registrationCreated);
        var service = new EventService(httpClient);

        // Act
        var result = await service.RegisterForEventAsync(eventId, userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(registrationCreated);
    }

    /// <summary>
    /// Tests that RegisterForEventAsync returns null when the API returns 404 Not Found.
    /// </summary>
    [Fact]
    public async Task RegisterForEventAsync_WhenApiReturns404_ReturnsNull()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var httpClient = CreateHttpClientWithRegistrationResponse(HttpStatusCode.NotFound, null);
        var service = new EventService(httpClient);

        // Act
        var result = await service.RegisterForEventAsync(eventId, userId);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Tests that RegisterForEventAsync returns null when the API returns 409 Conflict due to duplicate registration.
    /// </summary>
    [Fact]
    public async Task RegisterForEventAsync_WhenApiReturns409Conflict_ReturnsNull()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var httpClient = CreateHttpClientWithRegistrationResponse(HttpStatusCode.Conflict, null);
        var service = new EventService(httpClient);

        // Act
        var result = await service.RegisterForEventAsync(eventId, userId);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Tests that RegisterForEventAsync returns null when the API returns 400 Bad Request.
    /// </summary>
    [Fact]
    public async Task RegisterForEventAsync_WhenApiReturns400BadRequest_ReturnsNull()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var httpClient = CreateHttpClientWithRegistrationResponse(HttpStatusCode.BadRequest, null);
        var service = new EventService(httpClient);

        // Act
        var result = await service.RegisterForEventAsync(eventId, userId);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Tests that RegisterForEventAsync throws HttpRequestException when the API returns 500 Server Error.
    /// </summary>
    [Fact]
    public async Task RegisterForEventAsync_WhenApiReturns500_ThrowsHttpRequestException()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var httpClient = CreateHttpClientWithRegistrationResponse(HttpStatusCode.InternalServerError, null);
        var service = new EventService(httpClient);

        // Act
        var act = async () => await service.RegisterForEventAsync(eventId, userId);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    /// <summary>
    /// Tests that RegisterForEventAsync respects cancellation token.
    /// </summary>
    [Fact]
    public async Task RegisterForEventAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var httpClient = CreateHttpClientWithDelay();
        var service = new EventService(httpClient);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = async () => await service.RegisterForEventAsync(eventId, userId, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    /// <summary>
    /// Tests that CancelRegistrationAsync returns true when the API returns 204 No Content.
    /// </summary>
    [Fact]
    public async Task CancelRegistrationAsync_WhenApiReturns204_ReturnsTrue()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var httpClient = CreateHttpClientWithCancellationResponse(HttpStatusCode.NoContent);
        var service = new EventService(httpClient);

        // Act
        var result = await service.CancelRegistrationAsync(eventId, userId);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Tests that CancelRegistrationAsync returns false when the API returns 404 Not Found.
    /// </summary>
    [Fact]
    public async Task CancelRegistrationAsync_WhenApiReturns404_ReturnsFalse()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var httpClient = CreateHttpClientWithCancellationResponse(HttpStatusCode.NotFound);
        var service = new EventService(httpClient);

        // Act
        var result = await service.CancelRegistrationAsync(eventId, userId);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Tests that CancelRegistrationAsync returns false when the API returns 400 Bad Request.
    /// </summary>
    [Fact]
    public async Task CancelRegistrationAsync_WhenApiReturns400BadRequest_ReturnsFalse()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var httpClient = CreateHttpClientWithCancellationResponse(HttpStatusCode.BadRequest);
        var service = new EventService(httpClient);

        // Act
        var result = await service.CancelRegistrationAsync(eventId, userId);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Tests that CancelRegistrationAsync throws HttpRequestException when the API returns 500 Server Error.
    /// </summary>
    [Fact]
    public async Task CancelRegistrationAsync_WhenApiReturns500_ThrowsHttpRequestException()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var httpClient = CreateHttpClientWithCancellationResponse(HttpStatusCode.InternalServerError);
        var service = new EventService(httpClient);

        // Act
        var act = async () => await service.CancelRegistrationAsync(eventId, userId);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    /// <summary>
    /// Tests that CancelRegistrationAsync respects cancellation token.
    /// </summary>
    [Fact]
    public async Task CancelRegistrationAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var httpClient = CreateHttpClientWithDelay();
        var service = new EventService(httpClient);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = async () => await service.CancelRegistrationAsync(eventId, userId, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private static HttpClient CreateHttpClientWithRegistrationResponse(HttpStatusCode statusCode, RegistrationCreatedDto? registration)
    {
        var handler = new TestHttpMessageHandlerForRegistration(statusCode, registration);
        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost:7001")
        };
    }

    private static HttpClient CreateHttpClientWithCancellationResponse(HttpStatusCode statusCode)
    {
        var handler = new TestHttpMessageHandlerForCancellation(statusCode);
        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost:7001")
        };
    }

    private sealed class TestHttpMessageHandlerForRegistration(HttpStatusCode statusCode, RegistrationCreatedDto? registration) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(statusCode);
            if (statusCode == HttpStatusCode.Created && registration is not null)
            {
                response.Content = JsonContent.Create(registration);
            }
            return Task.FromResult(response);
        }
    }

    private sealed class TestHttpMessageHandlerForCancellation(HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(statusCode);
            return Task.FromResult(response);
        }
    }
}
