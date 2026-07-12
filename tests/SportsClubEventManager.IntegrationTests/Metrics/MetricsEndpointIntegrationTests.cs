using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SportsClubEventManager.Api.Models;
using SportsClubEventManager.Domain.Entities;
using SportsClubEventManager.Domain.Enums;
using SportsClubEventManager.Infrastructure.Persistence;

namespace SportsClubEventManager.IntegrationTests.Metrics;

/// <summary>
/// Integration tests for the Prometheus scrape endpoint (GET /metrics, issue #42). Verifies the
/// endpoint responds in the Prometheus text-exposition format and that invoking the real
/// registration/cancellation flow through the Api increases the corresponding business counter as
/// read back from a subsequent /metrics scrape. Follows the same DatabaseFixture +
/// WebApplicationFactory&lt;Program&gt; pattern used by the other Events integration tests in this
/// project (e.g. EventRegistrationIntegrationTests, issue #41's health endpoint tests).
/// </summary>
public class MetricsEndpointIntegrationTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetricsEndpointIntegrationTests"/> class.
    /// </summary>
    /// <param name="fixture">The database fixture providing SQL Server container access.</param>
    public MetricsEndpointIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Initializes the test web application factory and HTTP client before each test.
    /// </summary>
    /// <returns>A completed task.</returns>
    public Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove the existing AppDbContext registration
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    // Add AppDbContext using the test container connection string
                    services.AddDbContext<AppDbContext>(options =>
                        options.UseSqlServer(_fixture.ConnectionString));
                });
            });

        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Cleans up resources and resets the database after each test.
    /// </summary>
    /// <returns>A task representing the asynchronous cleanup operation.</returns>
    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_factory != null)
        {
            await _factory.DisposeAsync();
        }
        await _fixture.ResetDatabaseAsync();
    }

    /// <summary>
    /// Tests that verify the basic shape of the /metrics response.
    /// </summary>
    public sealed class WhenScrapingMetricsEndpoint : MetricsEndpointIntegrationTests
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WhenScrapingMetricsEndpoint"/> class.
        /// </summary>
        /// <param name="fixture">The database fixture.</param>
        public WhenScrapingMetricsEndpoint(DatabaseFixture fixture) : base(fixture)
        {
        }

        /// <summary>
        /// Verifies that GET /metrics returns 200 OK.
        /// </summary>
        [Fact]
        public async Task GetMetrics_WhenCalled_Returns200Ok()
        {
            // Arrange

            // Act
            var response = await _client.GetAsync("/metrics");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        /// <summary>
        /// Verifies that GET /metrics responds with the Prometheus text-exposition content type.
        /// </summary>
        [Fact]
        public async Task GetMetrics_WhenCalled_ReturnsPrometheusTextExpositionContentType()
        {
            // Arrange

            // Act
            var response = await _client.GetAsync("/metrics");

            // Assert
            response.Content.Headers.ContentType.Should().NotBeNull();
            response.Content.Headers.ContentType!.MediaType.Should().StartWith("text/plain");
        }

        /// <summary>
        /// Verifies that GET /metrics includes the default ASP.NET Core HTTP metrics registered by
        /// UseHttpMetrics(), confirming the middleware is wired up (AC 2: request instrumentation).
        /// </summary>
        [Fact]
        public async Task GetMetrics_WhenCalled_IncludesDefaultHttpRequestMetrics()
        {
            // Arrange
            // Issue an unrelated request first so at least one HTTP request has been recorded.
            await _client.GetAsync("/metrics");

            // Act
            var body = await GetMetricsBodyAsync();

            // Assert
            body.Should().Contain("http_requests_received_total");
        }

        /// <summary>
        /// Verifies that GET /metrics eventually includes the "sportsclubeventmanager_active_events"
        /// gauge (issue #42's third business metric). ActiveEventsGaugeUpdater's static Gauge field
        /// is only registered in prometheus-net's default collector registry the first time its
        /// ExecuteAsync loop actually reaches the "ActiveEvents.Set(count)" line, which happens
        /// asynchronously after its first (awaited) database query completes — so immediately after
        /// host startup the gauge may not have appeared yet. This polls /metrics for a few seconds
        /// instead of asserting on a single immediate scrape, to avoid a startup-timing race.
        /// </summary>
        [Fact]
        public async Task GetMetrics_WhenCalled_EventuallyIncludesActiveEventsGauge()
        {
            // Arrange
            var deadline = DateTime.UtcNow.AddSeconds(10);
            string body;

            // Act
            do
            {
                body = await GetMetricsBodyAsync();
                if (body.Contains("sportsclubeventmanager_active_events"))
                {
                    break;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(250));
            } while (DateTime.UtcNow < deadline);

            // Assert
            body.Should().Contain("sportsclubeventmanager_active_events");
        }
    }

    /// <summary>
    /// Tests that verify the registrations-created counter increases after a real registration
    /// flows through the Api.
    /// </summary>
    public sealed class WhenRegistrationFlowIsInvoked : MetricsEndpointIntegrationTests
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WhenRegistrationFlowIsInvoked"/> class.
        /// </summary>
        /// <param name="fixture">The database fixture.</param>
        public WhenRegistrationFlowIsInvoked(DatabaseFixture fixture) : base(fixture)
        {
        }

        /// <summary>
        /// Verifies that POST /api/v1/events/{id}/register, once it succeeds, increases the
        /// "sportsclubeventmanager_event_registrations_total" counter for the "self-service" label
        /// as observed on a subsequent /metrics scrape.
        /// </summary>
        [Fact]
        public async Task RegisterForEvent_WhenSuccessful_IncreasesSelfServiceRegistrationsCounter()
        {
            // Arrange
            var eventId = await SeedEventAsync("Basketball Tournament", 100);
            var userId = await SeedUserAsync("John Doe", "john.metrics@test.com");

            var valueBefore = await ReadCounterValueAsync(
                "sportsclubeventmanager_event_registrations_total", "source", "self-service");

            var request = new RegisterForEventRequest { UserId = userId };

            // Act
            var response = await _client.PostAsJsonAsync($"/api/v1/events/{eventId}/register", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var valueAfter = await ReadCounterValueAsync(
                "sportsclubeventmanager_event_registrations_total", "source", "self-service");
            valueAfter.Should().BeGreaterThan(valueBefore);
        }

        /// <summary>
        /// Verifies that a registration attempt that fails validation (past event date, so the
        /// handler throws before SaveChangesAsync) does not increase the registrations-created
        /// counter.
        /// </summary>
        [Fact]
        public async Task RegisterForEvent_WhenRequestFails_DoesNotIncreaseRegistrationsCounter()
        {
            // Arrange
            var eventId = await SeedPastEventAsync("Past Event", 50);
            var userId = await SeedUserAsync("Frank Miller", "frank.metrics@test.com");

            var valueBefore = await ReadCounterValueAsync(
                "sportsclubeventmanager_event_registrations_total", "source", "self-service");

            var request = new RegisterForEventRequest { UserId = userId };

            // Act
            var response = await _client.PostAsJsonAsync($"/api/v1/events/{eventId}/register", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var valueAfter = await ReadCounterValueAsync(
                "sportsclubeventmanager_event_registrations_total", "source", "self-service");
            valueAfter.Should().Be(valueBefore);
        }
    }

    /// <summary>
    /// Tests that verify the registrations-cancelled counter increases after a real cancellation
    /// flows through the Api.
    /// </summary>
    public sealed class WhenCancellationFlowIsInvoked : MetricsEndpointIntegrationTests
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WhenCancellationFlowIsInvoked"/> class.
        /// </summary>
        /// <param name="fixture">The database fixture.</param>
        public WhenCancellationFlowIsInvoked(DatabaseFixture fixture) : base(fixture)
        {
        }

        /// <summary>
        /// Verifies that DELETE /api/v1/events/{id}/register, once it succeeds, increases the
        /// "sportsclubeventmanager_registration_cancellations_total" counter for the
        /// "self-service" label as observed on a subsequent /metrics scrape.
        /// </summary>
        [Fact]
        public async Task CancelRegistration_WhenSuccessful_IncreasesSelfServiceCancellationsCounter()
        {
            // Arrange
            var eventId = await SeedEventAsync("Volleyball Match", 50);
            var userId = await SeedUserAsync("Jane Smith", "jane.metrics@test.com");
            await SeedRegistrationAsync(eventId, userId);

            var valueBefore = await ReadCounterValueAsync(
                "sportsclubeventmanager_registration_cancellations_total", "source", "self-service");

            var request = new CancelRegistrationRequest { UserId = userId };

            // Act
            var response = await _client.SendAsync(new HttpRequestMessage
            {
                Method = HttpMethod.Delete,
                RequestUri = new Uri($"/api/v1/events/{eventId}/register", UriKind.Relative),
                Content = JsonContent.Create(request)
            });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var valueAfter = await ReadCounterValueAsync(
                "sportsclubeventmanager_registration_cancellations_total", "source", "self-service");
            valueAfter.Should().BeGreaterThan(valueBefore);
        }
    }

    #region Helper Methods

    /// <summary>
    /// Retrieves the raw Prometheus text-exposition body from /metrics.
    /// </summary>
    /// <returns>The response body as plain text.</returns>
    private async Task<string> GetMetricsBodyAsync()
    {
        var response = await _client.GetAsync("/metrics");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Scrapes /metrics and parses the numeric value of a counter/gauge sample matching the given
    /// metric name and single label name/value pair (e.g. sportsclubeventmanager_event_registrations_total{source="self-service"} 3).
    /// Returns 0 if the series has not been observed yet (a Prometheus counter that has never been
    /// incremented for a given label combination simply does not appear in the exposition output).
    /// </summary>
    /// <param name="metricName">The Prometheus metric name.</param>
    /// <param name="labelName">The label name to match.</param>
    /// <param name="labelValue">The label value to match.</param>
    /// <returns>The current value of the matching sample, or 0 if not present.</returns>
    private async Task<double> ReadCounterValueAsync(string metricName, string labelName, string labelValue)
    {
        var body = await GetMetricsBodyAsync();

        var pattern = new Regex(
            $@"^{Regex.Escape(metricName)}\{{[^}}]*{Regex.Escape(labelName)}=""{Regex.Escape(labelValue)}""[^}}]*\}}\s+([0-9.eE+-]+)\s*$",
            RegexOptions.Multiline);

        var match = pattern.Match(body);
        return match.Success ? double.Parse(match.Groups[1].Value) : 0d;
    }

    /// <summary>
    /// Seeds a future event into the test database.
    /// </summary>
    /// <param name="title">The event title.</param>
    /// <param name="maxCapacity">The maximum capacity.</param>
    /// <returns>The ID of the created event.</returns>
    private async Task<Guid> SeedEventAsync(string title, int maxCapacity)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var eventEntity = new Event
        {
            Title = title,
            Date = DateTime.UtcNow.AddDays(7),
            Location = "Test Location",
            MaxCapacity = maxCapacity
        };

        context.Events.Add(eventEntity);
        await context.SaveChangesAsync();

        return eventEntity.Id;
    }

    /// <summary>
    /// Seeds a past event into the test database.
    /// </summary>
    /// <param name="title">The event title.</param>
    /// <param name="maxCapacity">The maximum capacity.</param>
    /// <returns>The ID of the created event.</returns>
    private async Task<Guid> SeedPastEventAsync(string title, int maxCapacity)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var eventEntity = new Event
        {
            Title = title,
            Date = DateTime.UtcNow.AddDays(-7),
            Location = "Test Location",
            MaxCapacity = maxCapacity
        };

        context.Events.Add(eventEntity);
        await context.SaveChangesAsync();

        return eventEntity.Id;
    }

    /// <summary>
    /// Seeds a user into the test database.
    /// </summary>
    /// <param name="name">The user name.</param>
    /// <param name="email">The user email.</param>
    /// <returns>The ID of the created user.</returns>
    private async Task<Guid> SeedUserAsync(string name, string email)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = new User
        {
            Name = name,
            Email = email,
            Gender = Gender.Male
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return user.Id;
    }

    /// <summary>
    /// Seeds an active registration into the test database.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <returns>The ID of the created registration.</returns>
    private async Task<Guid> SeedRegistrationAsync(Guid eventId, Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var registration = new Registration
        {
            EventId = eventId,
            UserId = userId,
            Status = RegistrationStatus.Registered,
            RegistrationDate = DateTime.UtcNow
        };

        context.Registrations.Add(registration);
        await context.SaveChangesAsync();

        return registration.Id;
    }

    #endregion
}
