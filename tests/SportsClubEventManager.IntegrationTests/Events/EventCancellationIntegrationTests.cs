using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SportsClubEventManager.Api.Models;
using SportsClubEventManager.Domain.Entities;
using SportsClubEventManager.Domain.Enums;
using SportsClubEventManager.Infrastructure.Persistence;

namespace SportsClubEventManager.IntegrationTests.Events;

/// <summary>
/// Integration tests for DELETE /api/v1/events/{id}/register endpoint.
/// Tests the full stack from HTTP request to database and back.
/// </summary>
public class EventCancellationIntegrationTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventCancellationIntegrationTests"/> class.
    /// </summary>
    /// <param name="fixture">The database fixture providing SQL Server container access.</param>
    public EventCancellationIntegrationTests(DatabaseFixture fixture)
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
    /// Tests that verify successful cancellation scenarios.
    /// </summary>
    public sealed class WhenCancellationIsSuccessful : EventCancellationIntegrationTests
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WhenCancellationIsSuccessful"/> class.
        /// </summary>
        /// <param name="fixture">The database fixture.</param>
        public WhenCancellationIsSuccessful(DatabaseFixture fixture) : base(fixture)
        {
        }

        /// <summary>
        /// Verifies that DELETE /api/v1/events/{id}/register returns 204 No Content.
        /// </summary>
        [Fact]
        public async Task CancelRegistration_WhenValidRequest_Returns204NoContent()
        {
            // Arrange
            var eventId = await SeedEventAsync("Basketball Tournament", "Annual tournament", 100);
            var userId = await SeedUserAsync("John Doe", "john@test.com");
            await SeedRegistrationAsync(eventId, userId, RegistrationStatus.Registered);

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
        }

        /// <summary>
        /// Verifies that registration is permanently removed from the database.
        /// </summary>
        [Fact]
        public async Task CancelRegistration_WhenValidRequest_RemovesRegistrationFromDatabase()
        {
            // Arrange
            var eventId = await SeedEventAsync("Volleyball Match", "Spring match", 50);
            var userId = await SeedUserAsync("Jane Smith", "jane@test.com");
            var registrationId = await SeedRegistrationAsync(eventId, userId, RegistrationStatus.Registered);

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

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var deletedRegistration = await context.Registrations
                .FirstOrDefaultAsync(r => r.Id == registrationId);

            deletedRegistration.Should().BeNull();
        }

        /// <summary>
        /// Verifies that cancelling one registration does not affect other registrations for the same event.
        /// </summary>
        [Fact]
        public async Task CancelRegistration_WhenMultipleRegistrationsExist_OnlyRemovesSpecificRegistration()
        {
            // Arrange
            var eventId = await SeedEventAsync("Yoga Class", "Morning yoga", 20);
            var userId1 = await SeedUserAsync("Alice Brown", "alice@test.com");
            var userId2 = await SeedUserAsync("Bob Wilson", "bob@test.com");
            await SeedRegistrationAsync(eventId, userId1, RegistrationStatus.Registered);
            var registration2Id = await SeedRegistrationAsync(eventId, userId2, RegistrationStatus.Registered);

            var request = new CancelRegistrationRequest { UserId = userId1 };

            // Act
            var response = await _client.SendAsync(new HttpRequestMessage
            {
                Method = HttpMethod.Delete,
                RequestUri = new Uri($"/api/v1/events/{eventId}/register", UriKind.Relative),
                Content = JsonContent.Create(request)
            });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var remainingRegistration = await context.Registrations
                .FirstOrDefaultAsync(r => r.Id == registration2Id);

            remainingRegistration.Should().NotBeNull();
            remainingRegistration!.Status.Should().Be(RegistrationStatus.Registered);
        }
    }

    /// <summary>
    /// Tests that verify error handling for non-existent resources.
    /// </summary>
    public sealed class WhenResourceDoesNotExist : EventCancellationIntegrationTests
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WhenResourceDoesNotExist"/> class.
        /// </summary>
        /// <param name="fixture">The database fixture.</param>
        public WhenResourceDoesNotExist(DatabaseFixture fixture) : base(fixture)
        {
        }

        /// <summary>
        /// Verifies that DELETE /api/v1/events/{id}/register returns 404 when event does not exist.
        /// </summary>
        [Fact]
        public async Task CancelRegistration_WhenEventDoesNotExist_Returns404()
        {
            // Arrange
            var nonExistentEventId = Guid.NewGuid();
            var userId = await SeedUserAsync("David Lee", "david@test.com");

            var request = new CancelRegistrationRequest { UserId = userId };

            // Act
            var response = await _client.SendAsync(new HttpRequestMessage
            {
                Method = HttpMethod.Delete,
                RequestUri = new Uri($"/api/v1/events/{nonExistentEventId}/register", UriKind.Relative),
                Content = JsonContent.Create(request)
            });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Event");
            content.Should().Contain("does not exist");
        }

        /// <summary>
        /// Verifies that attempting to cancel a non-existent registration returns 404.
        /// </summary>
        [Fact]
        public async Task CancelRegistration_WhenRegistrationDoesNotExist_Returns404()
        {
            // Arrange
            var eventId = await SeedEventAsync("Tennis Tournament", "Summer tennis", 40);
            var userId = await SeedUserAsync("Carol Davis", "carol@test.com");

            var request = new CancelRegistrationRequest { UserId = userId };

            // Act
            var response = await _client.SendAsync(new HttpRequestMessage
            {
                Method = HttpMethod.Delete,
                RequestUri = new Uri($"/api/v1/events/{eventId}/register", UriKind.Relative),
                Content = JsonContent.Create(request)
            });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("No active registration found");
        }

        /// <summary>
        /// Verifies that attempting to cancel an already cancelled registration returns 404.
        /// </summary>
        [Fact]
        public async Task CancelRegistration_WhenRegistrationIsAlreadyCancelled_Returns404()
        {
            // Arrange
            var eventId = await SeedEventAsync("Swimming Lesson", "Beginner swimming", 15);
            var userId = await SeedUserAsync("Emily White", "emily@test.com");
            await SeedRegistrationAsync(eventId, userId, RegistrationStatus.Cancelled);

            var request = new CancelRegistrationRequest { UserId = userId };

            // Act
            var response = await _client.SendAsync(new HttpRequestMessage
            {
                Method = HttpMethod.Delete,
                RequestUri = new Uri($"/api/v1/events/{eventId}/register", UriKind.Relative),
                Content = JsonContent.Create(request)
            });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("No active registration found");
        }
    }

    /// <summary>
    /// Tests that verify validation of invalid requests.
    /// </summary>
    public sealed class WhenRequestIsInvalid : EventCancellationIntegrationTests
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WhenRequestIsInvalid"/> class.
        /// </summary>
        /// <param name="fixture">The database fixture.</param>
        public WhenRequestIsInvalid(DatabaseFixture fixture) : base(fixture)
        {
        }

        /// <summary>
        /// Verifies that empty UserId returns 400 Bad Request.
        /// </summary>
        [Fact]
        public async Task CancelRegistration_WhenUserIdIsEmpty_Returns400()
        {
            // Arrange
            var eventId = await SeedEventAsync("Running Event", "Marathon prep", 30);
            var request = new CancelRegistrationRequest { UserId = Guid.Empty };

            // Act
            var response = await _client.SendAsync(new HttpRequestMessage
            {
                Method = HttpMethod.Delete,
                RequestUri = new Uri($"/api/v1/events/{eventId}/register", UriKind.Relative),
                Content = JsonContent.Create(request)
            });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        /// <summary>
        /// Verifies that invalid event ID format returns 400 Bad Request.
        /// </summary>
        [Fact]
        public async Task CancelRegistration_WhenEventIdIsNotValidGuid_Returns400()
        {
            // Arrange
            var userId = await SeedUserAsync("Frank Miller", "frank@test.com");
            var request = new CancelRegistrationRequest { UserId = userId };

            // Act
            var response = await _client.SendAsync(new HttpRequestMessage
            {
                Method = HttpMethod.Delete,
                RequestUri = new Uri("/api/v1/events/not-a-guid/register", UriKind.Relative),
                Content = JsonContent.Create(request)
            });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        /// <summary>
        /// Verifies that cancelling registration for a past event returns 400 Bad Request.
        /// </summary>
        [Fact]
        public async Task CancelRegistration_WhenEventDateIsInPast_Returns400()
        {
            // Arrange
            var eventId = await SeedPastEventAsync("Past Event", "This already happened", 50);
            var userId = await SeedUserAsync("George Taylor", "george@test.com");
            await SeedRegistrationAsync(eventId, userId, RegistrationStatus.Registered);

            var request = new CancelRegistrationRequest { UserId = userId };

            // Act
            var response = await _client.SendAsync(new HttpRequestMessage
            {
                Method = HttpMethod.Delete,
                RequestUri = new Uri($"/api/v1/events/{eventId}/register", UriKind.Relative),
                Content = JsonContent.Create(request)
            });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("already occurred");
        }
    }

    /// <summary>
    /// Tests that verify re-registration after cancellation.
    /// </summary>
    public sealed class WhenReRegisteringAfterCancellation : EventCancellationIntegrationTests
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WhenReRegisteringAfterCancellation"/> class.
        /// </summary>
        /// <param name="fixture">The database fixture.</param>
        public WhenReRegisteringAfterCancellation(DatabaseFixture fixture) : base(fixture)
        {
        }

        /// <summary>
        /// Verifies that a user can re-register for an event after cancelling their previous registration.
        /// </summary>
        [Fact]
        public async Task ReRegister_AfterCancellation_Returns201Created()
        {
            // Arrange
            var eventId = await SeedEventAsync("Cycling Event", "Weekend ride", 25);
            var userId = await SeedUserAsync("Helen Martinez", "helen@test.com");
            await SeedRegistrationAsync(eventId, userId, RegistrationStatus.Registered);

            var cancelRequest = new CancelRegistrationRequest { UserId = userId };

            // Act - Cancel registration
            var cancelResponse = await _client.SendAsync(new HttpRequestMessage
            {
                Method = HttpMethod.Delete,
                RequestUri = new Uri($"/api/v1/events/{eventId}/register", UriKind.Relative),
                Content = JsonContent.Create(cancelRequest)
            });

            cancelResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Act - Re-register
            var registerRequest = new RegisterForEventRequest { UserId = userId };
            var registerResponse = await _client.PostAsJsonAsync($"/api/v1/events/{eventId}/register", registerRequest);

            // Assert
            registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
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
