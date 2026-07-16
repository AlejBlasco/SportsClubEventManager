using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SportsClubEventManager.Api.Models;
using SportsClubEventManager.Domain.Entities;
using SportsClubEventManager.Domain.Enums;
using SportsClubEventManager.Infrastructure.Persistence;
using SportsClubEventManager.Shared.DTOs;

namespace SportsClubEventManager.IntegrationTests.Events;

/// <summary>
/// Integration tests for POST /api/v1/events/{id}/register endpoint.
/// Tests the full stack from HTTP request to database and back.
/// </summary>
public class EventRegistrationIntegrationTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventRegistrationIntegrationTests"/> class.
    /// </summary>
    /// <param name="fixture">The database fixture providing SQL Server container access.</param>
    public EventRegistrationIntegrationTests(DatabaseFixture fixture)
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
    /// Tests that verify successful registration scenarios.
    /// </summary>
    public sealed class WhenRegistrationIsSuccessful : EventRegistrationIntegrationTests
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WhenRegistrationIsSuccessful"/> class.
        /// </summary>
        /// <param name="fixture">The database fixture.</param>
        public WhenRegistrationIsSuccessful(DatabaseFixture fixture) : base(fixture)
        {
        }

        /// <summary>
        /// Verifies that POST /api/v1/events/{id}/register returns 201 Created with registration details.
        /// </summary>
        [Fact]
        public async Task RegisterForEvent_WhenValidRequest_Returns201WithRegistrationDetails()
        {
            // Arrange
            var eventId = await SeedEventAsync("Basketball Tournament", "Annual tournament", 100);
            var userId = await SeedUserAsync("John Doe", "john@test.com");
            await AuthenticateAsAsync("john@test.com");

            var request = new RegisterForEventRequest { UserId = userId };

            // Act
            var response = await _client.PostAsJsonAsync($"/api/v1/events/{eventId}/register", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var result = await response.Content.ReadFromJsonAsync<RegistrationCreatedDto>();
            result.Should().NotBeNull();
            result!.RegistrationId.Should().NotBeEmpty();
            result.EventId.Should().Be(eventId);
            result.UserId.Should().Be(userId);
            result.Status.Should().Be(RegistrationStatus.Registered);
            result.Event.Should().NotBeNull();
            result.Event.Title.Should().Be("Basketball Tournament");
            result.Event.CurrentRegistrations.Should().Be(1);
            result.Event.AvailableSlots.Should().Be(99);
            result.Event.IsFullyBooked.Should().BeFalse();
        }

        /// <summary>
        /// Verifies that registration is persisted correctly to the database.
        /// </summary>
        [Fact]
        public async Task RegisterForEvent_WhenValidRequest_PersistsRegistrationToDatabase()
        {
            // Arrange
            var eventId = await SeedEventAsync("Volleyball Match", "Spring match", 50);
            var userId = await SeedUserAsync("Jane Smith", "jane@test.com");
            await AuthenticateAsAsync("jane@test.com");

            var request = new RegisterForEventRequest { UserId = userId };

            // Act
            var response = await _client.PostAsJsonAsync($"/api/v1/events/{eventId}/register", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var result = await response.Content.ReadFromJsonAsync<RegistrationCreatedDto>();

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var persistedRegistration = await context.Registrations
                .FirstOrDefaultAsync(r => r.Id == result!.RegistrationId);

            persistedRegistration.Should().NotBeNull();
            persistedRegistration!.EventId.Should().Be(eventId);
            persistedRegistration.UserId.Should().Be(userId);
            persistedRegistration.Status.Should().Be(RegistrationStatus.Registered);
        }

        /// <summary>
        /// Verifies that Location header is included in the response.
        /// </summary>
        [Fact]
        public async Task RegisterForEvent_WhenValidRequest_ReturnsLocationHeader()
        {
            // Arrange
            var eventId = await SeedEventAsync("Yoga Class", "Morning yoga", 20);
            var userId = await SeedUserAsync("Alice Brown", "alice@test.com");
            await AuthenticateAsAsync("alice@test.com");

            var request = new RegisterForEventRequest { UserId = userId };

            // Act
            var response = await _client.PostAsJsonAsync($"/api/v1/events/{eventId}/register", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            response.Headers.Location.Should().NotBeNull();
        }
    }

    /// <summary>
    /// Tests that verify conflict scenarios.
    /// </summary>
    public sealed class WhenDuplicateRegistration : EventRegistrationIntegrationTests
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WhenDuplicateRegistration"/> class.
        /// </summary>
        /// <param name="fixture">The database fixture.</param>
        public WhenDuplicateRegistration(DatabaseFixture fixture) : base(fixture)
        {
        }

        /// <summary>
        /// Verifies that attempting to register twice for the same event returns 409 Conflict.
        /// </summary>
        [Fact]
        public async Task RegisterForEvent_WhenUserAlreadyRegistered_Returns409Conflict()
        {
            // Arrange
            var eventId = await SeedEventAsync("Tennis Tournament", "Summer tennis", 40);
            var userId = await SeedUserAsync("Bob Wilson", "bob@test.com");
            await AuthenticateAsAsync("bob@test.com");

            // Create initial registration
            await SeedRegistrationAsync(eventId, userId, RegistrationStatus.Registered);

            var request = new RegisterForEventRequest { UserId = userId };

            // Act
            var response = await _client.PostAsJsonAsync($"/api/v1/events/{eventId}/register", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("already registered");
        }

        /// <summary>
        /// Verifies that re-registration is allowed after a previous cancellation.
        /// </summary>
        [Fact]
        public async Task RegisterForEvent_WhenPreviousRegistrationWasCancelled_Returns201()
        {
            // Arrange
            var eventId = await SeedEventAsync("Swimming Lesson", "Beginner swimming", 15);
            var userId = await SeedUserAsync("Carol Davis", "carol@test.com");
            await AuthenticateAsAsync("carol@test.com");

            // Create cancelled registration
            await SeedRegistrationAsync(eventId, userId, RegistrationStatus.Cancelled);

            var request = new RegisterForEventRequest { UserId = userId };

            // Act
            var response = await _client.PostAsJsonAsync($"/api/v1/events/{eventId}/register", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }
    }

    /// <summary>
    /// Tests that verify error handling for non-existent events.
    /// </summary>
    public sealed class WhenEventDoesNotExist : EventRegistrationIntegrationTests
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WhenEventDoesNotExist"/> class.
        /// </summary>
        /// <param name="fixture">The database fixture.</param>
        public WhenEventDoesNotExist(DatabaseFixture fixture) : base(fixture)
        {
        }

        /// <summary>
        /// Verifies that POST /api/v1/events/{id}/register returns 404 when event does not exist.
        /// </summary>
        [Fact]
        public async Task RegisterForEvent_WhenEventDoesNotExist_Returns404()
        {
            // Arrange
            var nonExistentEventId = Guid.NewGuid();
            var userId = await SeedUserAsync("David Lee", "david@test.com");
            await AuthenticateAsAsync("david@test.com");

            var request = new RegisterForEventRequest { UserId = userId };

            // Act
            var response = await _client.PostAsJsonAsync($"/api/v1/events/{nonExistentEventId}/register", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Event");
            content.Should().Contain("does not exist");
        }
    }

    /// <summary>
    /// Tests that verify validation of invalid requests.
    /// </summary>
    public sealed class WhenRequestIsInvalid : EventRegistrationIntegrationTests
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WhenRequestIsInvalid"/> class.
        /// </summary>
        /// <param name="fixture">The database fixture.</param>
        public WhenRequestIsInvalid(DatabaseFixture fixture) : base(fixture)
        {
        }

        /// <summary>
        /// Verifies that an invalid event ID format returns 404 Not Found, since the route's
        /// {id:guid} constraint means a non-GUID segment simply doesn't match this endpoint
        /// at all - routing falls through to the generic "no endpoint matched" 404 before any
        /// action code (that could return 400) ever runs.
        /// </summary>
        [Fact]
        public async Task RegisterForEvent_WhenEventIdIsNotValidGuid_Returns404()
        {
            // Arrange
            var userId = await SeedUserAsync("Emily White", "emily@test.com");
            var request = new RegisterForEventRequest { UserId = userId };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/events/not-a-guid/register", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        /// <summary>
        /// Verifies that registering for a past event returns 400 Bad Request.
        /// </summary>
        [Fact]
        public async Task RegisterForEvent_WhenEventDateIsInPast_Returns400()
        {
            // Arrange
            var eventId = await SeedPastEventAsync("Past Event", "This already happened", 50);
            var userId = await SeedUserAsync("Frank Miller", "frank@test.com");
            await AuthenticateAsAsync("frank@test.com");

            var request = new RegisterForEventRequest { UserId = userId };

            // Act
            var response = await _client.PostAsJsonAsync($"/api/v1/events/{eventId}/register", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("already occurred");
        }
    }

    /// <summary>
    /// Tests that verify capacity enforcement.
    /// </summary>
    public sealed class WhenEventIsAtFullCapacity : EventRegistrationIntegrationTests
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WhenEventIsAtFullCapacity"/> class.
        /// </summary>
        /// <param name="fixture">The database fixture.</param>
        public WhenEventIsAtFullCapacity(DatabaseFixture fixture) : base(fixture)
        {
        }

        /// <summary>
        /// Verifies that attempting to register when event is at full capacity returns 409 Conflict.
        /// </summary>
        [Fact]
        public async Task RegisterForEvent_WhenEventIsAtMaxCapacity_Returns409()
        {
            // Arrange
            const int maxCapacity = 5;
            var eventId = await SeedEventAsync("Small Event", "Limited seats", maxCapacity);

            // Fill the event to capacity
            for (int i = 0; i < maxCapacity; i++)
            {
                var existingUserId = await SeedUserAsync($"User{i}", $"user{i}@test.com");
                await SeedRegistrationAsync(eventId, existingUserId, RegistrationStatus.Registered);
            }

            var newUserId = await SeedUserAsync("Late User", "late@test.com");
            await AuthenticateAsAsync("late@test.com");
            var request = new RegisterForEventRequest { UserId = newUserId };

            // Act
            var response = await _client.PostAsJsonAsync($"/api/v1/events/{eventId}/register", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("maximum capacity");
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
    /// Seeds a past event into the test database.
    /// </summary>
    /// <param name="title">The event title.</param>
    /// <param name="description">The event description.</param>
    /// <param name="maxCapacity">The maximum capacity.</param>
    /// <returns>The ID of the created event.</returns>
    private async Task<Guid> SeedPastEventAsync(string title, string? description, int maxCapacity)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var eventEntity = new Event
        {
            Title = title,
            Description = description,
            Date = DateTime.UtcNow.AddDays(-7),
            Location = "Test Location",
            MaxCapacity = maxCapacity
        };

        context.Events.Add(eventEntity);
        await context.SaveChangesAsync();

        return eventEntity.Id;
    }

    /// <summary>
    /// Password shared by every user this class seeds - registration/cancellation endpoints are
    /// [Authorize]-protected and derive the acting UserId from the caller's JWT, not from the
    /// request body, so every test needs a real, authenticatable user (see AuthenticateAsAsync).
    /// </summary>
    private const string TestPassword = "TestPass123!";

    /// <summary>
    /// Seeds a user into the test database, with a hashed password so it can log in via
    /// AuthenticateAsAsync.
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
            Gender = Gender.Male,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(TestPassword),
            ProviderName = "Local"
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return user.Id;
    }

    /// <summary>
    /// Logs in as the given user and attaches the resulting JWT to <see cref="_client"/> as a
    /// Bearer token, so subsequent requests are authenticated as that user.
    /// </summary>
    /// <param name="email">The email of a user previously seeded via <see cref="SeedUserAsync"/>.</param>
    private async Task AuthenticateAsAsync(string email)
    {
        var loginRequest = new LoginRequest { Email = email, Password = TestPassword };
        var response = await _client.PostAsJsonAsync("/api/authentication/login", loginRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK, "test setup requires a valid login");

        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResponse!.AccessToken);
    }

    /// <summary>
    /// Seeds a registration into the test database.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="status">The registration status.</param>
    /// <returns>The ID of the created registration.</returns>
    private async Task<Guid> SeedRegistrationAsync(Guid eventId, Guid userId, RegistrationStatus status)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var registration = new Registration
        {
            EventId = eventId,
            UserId = userId,
            Status = status,
            RegistrationDate = DateTime.UtcNow
        };

        context.Registrations.Add(registration);
        await context.SaveChangesAsync();

        return registration.Id;
    }

    #endregion
}
