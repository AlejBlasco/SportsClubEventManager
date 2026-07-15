using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SportsClubEventManager.Domain.Entities;
using SportsClubEventManager.Domain.Enums;
using SportsClubEventManager.Infrastructure.Persistence;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.IntegrationTests.Events;

/// <summary>
/// Integration tests for GET /api/v1/events/{id} endpoint.
/// Tests the full stack from HTTP request to database and back.
/// </summary>
public class GetEventByIdEndpointTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetEventByIdEndpointTests"/> class.
    /// </summary>
    /// <param name="fixture">The database fixture providing SQL Server container access.</param>
    public GetEventByIdEndpointTests(DatabaseFixture fixture)
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
    /// Tests that verify successful retrieval of event details.
    /// </summary>
    public sealed class WhenEventExists : GetEventByIdEndpointTests
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WhenEventExists"/> class.
        /// </summary>
        /// <param name="fixture">The database fixture.</param>
        public WhenEventExists(DatabaseFixture fixture) : base(fixture)
        {
        }

        /// <summary>
        /// Verifies that GET /api/v1/events/{id} returns 200 OK with event details when event exists.
        /// </summary>
        [Fact]
        public async Task GetEventById_WhenEventExists_Returns200WithEventDetails()
        {
            // Arrange
            var eventId = await SeedEventAsync("Basketball Tournament", "Annual basketball event", 100);

            // Act
            var response = await _client.GetAsync($"/api/v1/events/{eventId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var eventDetail = await response.Content.ReadFromJsonAsync<EventDetailDto>();
            eventDetail.Should().NotBeNull();
            eventDetail!.Id.Should().Be(eventId);
            eventDetail.Title.Should().Be("Basketball Tournament");
            eventDetail.Description.Should().Be("Annual basketball event");
            eventDetail.MaxCapacity.Should().Be(100);
        }

        /// <summary>
        /// Verifies that response includes all calculated fields correctly.
        /// </summary>
        [Fact]
        public async Task GetEventById_WhenEventHasRegistrations_ReturnsCalculatedFields()
        {
            // Arrange
            var eventId = await SeedEventAsync("Volleyball Match", "Spring volleyball match", 50);
            await SeedRegistrationsAsync(eventId, RegistrationStatus.Registered, 30);
            await SeedRegistrationsAsync(eventId, RegistrationStatus.Cancelled, 5);

            // Act
            var response = await _client.GetAsync($"/api/v1/events/{eventId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var eventDetail = await response.Content.ReadFromJsonAsync<EventDetailDto>();
            eventDetail.Should().NotBeNull();
            eventDetail!.CurrentRegistrations.Should().Be(30);
            eventDetail.AvailableSlots.Should().Be(20);
            eventDetail.IsFullyBooked.Should().BeFalse();
        }

        /// <summary>
        /// Verifies that IsFullyBooked is true when event is at capacity.
        /// </summary>
        [Fact]
        public async Task GetEventById_WhenEventIsAtCapacity_ReturnsIsFullyBookedTrue()
        {
            // Arrange
            var eventId = await SeedEventAsync("Yoga Class", "Morning yoga session", 20);
            await SeedRegistrationsAsync(eventId, RegistrationStatus.Registered, 20);

            // Act
            var response = await _client.GetAsync($"/api/v1/events/{eventId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var eventDetail = await response.Content.ReadFromJsonAsync<EventDetailDto>();
            eventDetail.Should().NotBeNull();
            eventDetail!.IsFullyBooked.Should().BeTrue();
            eventDetail.AvailableSlots.Should().Be(0);
        }

        /// <summary>
        /// Verifies that waitlisted registrations are included in capacity calculations.
        /// </summary>
        [Fact]
        public async Task GetEventById_WhenEventHasWaitlistedRegistrations_IncludesThemInCalculations()
        {
            // Arrange
            var eventId = await SeedEventAsync("Tennis Tournament", "Summer tennis event", 40);
            await SeedRegistrationsAsync(eventId, RegistrationStatus.Registered, 25);
            await SeedRegistrationsAsync(eventId, RegistrationStatus.Waitlisted, 10);

            // Act
            var response = await _client.GetAsync($"/api/v1/events/{eventId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var eventDetail = await response.Content.ReadFromJsonAsync<EventDetailDto>();
            eventDetail.Should().NotBeNull();
            eventDetail!.CurrentRegistrations.Should().Be(35);
            eventDetail.AvailableSlots.Should().Be(5);
        }

        /// <summary>
        /// Verifies that event with null description returns null for that field.
        /// </summary>
        [Fact]
        public async Task GetEventById_WhenDescriptionIsNull_ReturnsNullDescription()
        {
            // Arrange
            var eventId = await SeedEventAsync("Event Without Description", null, 50);

            // Act
            var response = await _client.GetAsync($"/api/v1/events/{eventId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var eventDetail = await response.Content.ReadFromJsonAsync<EventDetailDto>();
            eventDetail.Should().NotBeNull();
            eventDetail!.Description.Should().BeNull();
        }
    }

    /// <summary>
    /// Tests that verify error handling for non-existent events.
    /// </summary>
    public sealed class WhenEventDoesNotExist : GetEventByIdEndpointTests
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WhenEventDoesNotExist"/> class.
        /// </summary>
        /// <param name="fixture">The database fixture.</param>
        public WhenEventDoesNotExist(DatabaseFixture fixture) : base(fixture)
        {
        }

        /// <summary>
        /// Verifies that GET /api/v1/events/{id} returns 404 Not Found when event does not exist.
        /// </summary>
        [Fact]
        public async Task GetEventById_WhenEventDoesNotExist_Returns404()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/v1/events/{nonExistentId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        /// <summary>
        /// Verifies that 404 response includes ProblemDetails with meaningful error message.
        /// </summary>
        [Fact]
        public async Task GetEventById_WhenEventDoesNotExist_ReturnsProblemDetails()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/v1/events/{nonExistentId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Event not found");
            content.Should().Contain(nonExistentId.ToString());
        }
    }

    /// <summary>
    /// Tests that verify validation of invalid GUID formats.
    /// </summary>
    public sealed class WhenEventIdIsInvalid : GetEventByIdEndpointTests
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WhenEventIdIsInvalid"/> class.
        /// </summary>
        /// <param name="fixture">The database fixture.</param>
        public WhenEventIdIsInvalid(DatabaseFixture fixture) : base(fixture)
        {
        }

        /// <summary>
        /// Verifies that an invalid GUID format returns 404 Not Found, since the route's
        /// {id:guid} constraint means a non-GUID segment simply doesn't match this endpoint
        /// at all - routing falls through to the generic "no endpoint matched" 404 before any
        /// action code (that could return 400) ever runs.
        /// </summary>
        [Fact]
        public async Task GetEventById_WhenEventIdIsNotValidGuid_Returns404()
        {
            // Arrange & Act
            var response = await _client.GetAsync("/api/v1/events/not-a-guid");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        /// <summary>
        /// Verifies that empty GUID returns 400 Bad Request.
        /// </summary>
        [Fact]
        public async Task GetEventById_WhenEventIdIsEmpty_Returns400()
        {
            // Arrange & Act
            var response = await _client.GetAsync($"/api/v1/events/{Guid.Empty}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }

    #region Helper Methods

    /// <summary>
    /// Seeds an event into the test database.
    /// </summary>
    /// <param name="title">The event title.</param>
    /// <param name="description">The event description.</param>
    /// <param name="maxCapacity">The maximum capacity.</param>
    /// <returns>The ID of the created event.</returns>
    private async Task<Guid> SeedEventAsync(string title, string? description, int maxCapacity)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var eventEntity = new Event
        {
            Title = title,
            Description = description,
            Date = DateTime.UtcNow.AddDays(7),
            Location = "Test Location",
            MaxCapacity = maxCapacity
        };

        context.Events.Add(eventEntity);
        await context.SaveChangesAsync();

        return eventEntity.Id;
    }

    /// <summary>
    /// Seeds registrations for a specific event.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="status">The registration status.</param>
    /// <param name="count">The number of registrations to create.</param>
    private async Task SeedRegistrationsAsync(Guid eventId, RegistrationStatus status, int count)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        for (int i = 0; i < count; i++)
        {
            var user = new User
            {
                Name = $"Test User {Guid.NewGuid()}",
                Email = $"user{Guid.NewGuid()}@test.com",
                Gender = Gender.Male
            };

            var registration = new Registration
            {
                EventId = eventId,
                User = user,
                Status = status
            };

            context.Registrations.Add(registration);
        }

        await context.SaveChangesAsync();
    }

    #endregion
}
