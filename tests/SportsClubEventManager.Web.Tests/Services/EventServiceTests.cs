using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
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

    private sealed class DelayHttpMessageHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
