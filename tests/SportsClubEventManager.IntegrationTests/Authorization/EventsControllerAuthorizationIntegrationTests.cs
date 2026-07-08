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

namespace SportsClubEventManager.IntegrationTests.Authorization;

/// <summary>
/// Integration tests for authorization on Events endpoints, specifically testing
/// user-ownership validation and role-based access control for registration and cancellation.
/// </summary>
public class EventsControllerAuthorizationIntegrationTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventsControllerAuthorizationIntegrationTests"/> class.
    /// </summary>
    /// <param name="fixture">The database fixture providing SQL Server container access.</param>
    public EventsControllerAuthorizationIntegrationTests(DatabaseFixture fixture)
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
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

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
    /// Tests successful event registration with proper user ownership validation.
    /// </summary>
    public sealed class WhenRegisteringForEventWithProperAuthorization : EventsControllerAuthorizationIntegrationTests
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WhenRegisteringForEventWithProperAuthorization"/> class.
        /// </summary>
        /// <param name="fixture">The database fixture.</param>
        public WhenRegisteringForEventWithProperAuthorization(DatabaseFixture fixture) : base(fixture)
        {
        }

        /// <summary>
        /// Verifies that an authenticated user can register for an event using their own UserId.
        /// </summary>
        [Fact]
        public async Task RegisterForEvent_WithUserRoleAndOwnUserId_Returns201Created()
        {
            // Arrange
            var userId = await SeedUserWithPasswordAsync("eventuser@example.com", "User123", Role.User);
            var eventId = await SeedEventAsync("Test Event", "A test event", 50);

            var jwtToken = await LoginAndGetTokenAsync("eventuser@example.com", "User123");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

            var request = new RegisterForEventRequest { UserId = userId };

            // Act
            var response = await _client.PostAsJsonAsync($"/api/v1/events/{eventId}/register", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var result = await response.Content.ReadFromJsonAsync<RegistrationCreatedDto>();
            result.Should().NotBeNull();
            result!.UserId.Should().Be(userId);
            result.Status.Should().Be(RegistrationStatus.Registered);
        }

        /// <summary>
        /// Verifies that an administrator can register for an event using any UserId.
        /// </summary>
        [Fact]
        public async Task RegisterForEvent_WithAdministratorRoleAndAnyUserId_Returns201Created()
        {
            // Arrange
            var adminId = await SeedUserWithPasswordAsync("admin@example.com", "Admin123", Role.Administrator);
            var targetUserId = await SeedUserAsync("target@example.com", Gender.Female);
            var eventId = await SeedEventAsync("Admin Event", "Event for admin test", 50);

            var jwtToken = await LoginAndGetTokenAsync("admin@example.com", "Admin123");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

            var request = new RegisterForEventRequest { UserId = targetUserId };

            // Act
            var response = await _client.PostAsJsonAsync($"/api/v1/events/{eventId}/register", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var result = await response.Content.ReadFromJsonAsync<RegistrationCreatedDto>();
            result.Should().NotBeNull();
            result!.UserId.Should().Be(targetUserId);
        }
    }

    /// <summary>
    /// Tests authorization failures when users attempt to register on behalf of others.
    /// </summary>
    public sealed class WhenRegisteringForEventWithImproperAuthorization : EventsControllerAuthorizationIntegrationTests
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WhenRegisteringForEventWithImproperAuthorization"/> class.
        /// </summary>
        /// <param name="fixture">The database fixture.</param>
        public WhenRegisteringForEventWithImproperAuthorization(DatabaseFixture fixture) : base(fixture)
        {
        }

        /// <summary>
        /// Verifies that a User role cannot register another user for an event.
        /// </summary>
        [Fact]
        public async Task RegisterForEvent_WithUserRoleAndDifferentUserId_Returns403Forbidden()
        {
            // Arrange
            var authUserid = await SeedUserWithPasswordAsync("authuser@example.com", "User123", Role.User);
            var otherUserId = await SeedUserAsync("otheruser@example.com", Gender.Male);
            var eventId = await SeedEventAsync("Restricted Event", "Only own registration allowed", 50);

            var jwtToken = await LoginAndGetTokenAsync("authuser@example.com", "User123");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

            var request = new RegisterForEventRequest { UserId = otherUserId };

            // Act
            var response = await _client.PostAsJsonAsync($"/api/v1/events/{eventId}/register", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        /// <summary>
        /// Verifies that attempting to register with an invalid UserId returns 400 or 403.
        /// </summary>
        [Fact]
        public async Task RegisterForEvent_WithEmptyUserId_ReturnsBadRequest()
        {
            // Arrange
            await SeedUserWithPasswordAsync("validuser@example.com", "User123", Role.User);
            var eventId = await SeedEventAsync("Event", "Test", 50);

            var jwtToken = await LoginAndGetTokenAsync("validuser@example.com", "User123");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

            var request = new RegisterForEventRequest { UserId = Guid.Empty };

            // Act
            var response = await _client.PostAsJsonAsync($"/api/v1/events/{eventId}/register", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        /// <summary>
        /// Verifies that unauthenticated requests to register return 401 Unauthorized.
        /// </summary>
        [Fact]
        public async Task RegisterForEvent_WithoutAuthentication_Returns401Unauthorized()
        {
            // Arrange
            var userId = await SeedUserAsync("unauthuser@example.com", Gender.Male);
            var eventId = await SeedEventAsync("Event", "Test", 50);

            // No authorization header set
            var request = new RegisterForEventRequest { UserId = userId };

            // Act
            var response = await _client.PostAsJsonAsync($"/api/v1/events/{eventId}/register", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }

    /// <summary>
    /// Tests authorization for event registration cancellation.
    /// </summary>
    public sealed class WhenCancellingEventRegistration : EventsControllerAuthorizationIntegrationTests
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WhenCancellingEventRegistration"/> class.
        /// </summary>
        /// <param name="fixture">The database fixture.</param>
        public WhenCancellingEventRegistration(DatabaseFixture fixture) : base(fixture)
        {
        }

        /// <summary>
        /// Verifies that a user can cancel their own event registration.
        /// </summary>
        [Fact]
        public async Task CancelRegistration_WithUserRoleForOwnRegistration_Returns200OK()
        {
            // Arrange
            var userId = await SeedUserWithPasswordAsync("canceluser@example.com", "User123", Role.User);
            var eventId = await SeedEventAsync("Event to Cancel", "Cancellation test", 50);
            var registrationId = await SeedRegistrationAsync(eventId, userId, RegistrationStatus.Registered);

            var jwtToken = await LoginAndGetTokenAsync("canceluser@example.com", "User123");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

            var request = new CancelRegistrationRequest { UserId = userId };

            // Act
            var response = await _client.PostAsJsonAsync(
                $"/api/v1/events/{eventId}/registrations/{registrationId}/cancel",
                request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        /// <summary>
        /// Verifies that an administrator can cancel any user's event registration.
        /// </summary>
        [Fact]
        public async Task CancelRegistration_WithAdministratorRole_Returns200OK()
        {
            // Arrange
            var adminId = await SeedUserWithPasswordAsync("admin@example.com", "Admin123", Role.Administrator);
            var userId = await SeedUserAsync("targetuser@example.com", Gender.Male);
            var eventId = await SeedEventAsync("Event to Cancel", "Cancellation test", 50);
            var registrationId = await SeedRegistrationAsync(eventId, userId, RegistrationStatus.Registered);

            var jwtToken = await LoginAndGetTokenAsync("admin@example.com", "Admin123");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

            var request = new CancelRegistrationRequest { UserId = userId };

            // Act
            var response = await _client.PostAsJsonAsync(
                $"/api/v1/events/{eventId}/registrations/{registrationId}/cancel",
                request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        /// <summary>
        /// Verifies that a user cannot cancel another user's event registration.
        /// </summary>
        [Fact]
        public async Task CancelRegistration_WithUserRoleForAnotherUserRegistration_Returns403Forbidden()
        {
            // Arrange
            var userId = await SeedUserWithPasswordAsync("user1@example.com", "User123", Role.User);
            var otherUserId = await SeedUserAsync("user2@example.com", Gender.Male);
            var eventId = await SeedEventAsync("Event to Cancel", "Cancellation test", 50);
            var registrationId = await SeedRegistrationAsync(eventId, otherUserId, RegistrationStatus.Registered);

            var jwtToken = await LoginAndGetTokenAsync("user1@example.com", "User123");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

            var request = new CancelRegistrationRequest { UserId = otherUserId };

            // Act
            var response = await _client.PostAsJsonAsync(
                $"/api/v1/events/{eventId}/registrations/{registrationId}/cancel",
                request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        /// <summary>
        /// Verifies that unauthenticated requests to cancel registration return 401 Unauthorized.
        /// </summary>
        [Fact]
        public async Task CancelRegistration_WithoutAuthentication_Returns401Unauthorized()
        {
            // Arrange
            var userId = await SeedUserAsync("unauthuser@example.com", Gender.Male);
            var eventId = await SeedEventAsync("Event", "Test", 50);
            var registrationId = await SeedRegistrationAsync(eventId, userId, RegistrationStatus.Registered);

            // No authorization header
            var request = new CancelRegistrationRequest { UserId = userId };

            // Act
            var response = await _client.PostAsJsonAsync(
                $"/api/v1/events/{eventId}/registrations/{registrationId}/cancel",
                request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }

    #region Helper Methods

    /// <summary>
    /// Seeds a user with a hashed password and specified role.
    /// </summary>
    /// <param name="email">The user email.</param>
    /// <param name="password">The plain-text password.</param>
    /// <param name="role">The user role.</param>
    /// <returns>The ID of the created user.</returns>
    private async Task<Guid> SeedUserWithPasswordAsync(string email, string password, Role role)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

        var user = new User
        {
            Name = email.Split("@")[0],
            Email = email,
            Gender = Gender.Male,
            PasswordHash = hashedPassword,
            ProviderName = "Local",
            Role = role
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return user.Id;
    }

    /// <summary>
    /// Seeds a user without password (for OAuth2 or test data).
    /// </summary>
    /// <param name="email">The user email.</param>
    /// <param name="gender">The user gender.</param>
    /// <returns>The ID of the created user.</returns>
    private async Task<Guid> SeedUserAsync(string email, Gender gender)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = new User
        {
            Name = email.Split("@")[0],
            Email = email,
            Gender = gender,
            Role = Role.User
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return user.Id;
    }

    /// <summary>
    /// Seeds an event into the test database.
    /// </summary>
    /// <param name="title">The event title.</param>
    /// <param name="description">The event description.</param>
    /// <param name="maxCapacity">The maximum capacity.</param>
    /// <returns>The ID of the created event.</returns>
    private async Task<Guid> SeedEventAsync(string title, string description, int maxCapacity)
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

    /// <summary>
    /// Logs in a user and returns the JWT access token.
    /// </summary>
    /// <param name="email">The user email.</param>
    /// <param name="password">The user password.</param>
    /// <returns>The JWT access token string.</returns>
    private async Task<string> LoginAndGetTokenAsync(string email, string password)
    {
        var loginRequest = new LoginRequest
        {
            Email = email,
            Password = password
        };

        var response = await _client.PostAsJsonAsync("/api/authentication/login", loginRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return loginResponse!.AccessToken;
    }

    #endregion
}
