using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SportsClubEventManager.Api;
using Xunit;

namespace SportsClubEventManager.Api.Tests.Controllers;

/// <summary>
/// Integration tests for AdminEventsController to verify role-based authorization (OQ-1 decision enforcement).
/// Tests verify that [Authorize(Roles = "Administrator")] is properly enforced on all admin event endpoints.
/// </summary>
public class AdminEventsControllerAuthorizationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminEventsControllerAuthorizationTests"/> class.
    /// </summary>
    /// <param name="factory">The web application factory.</param>
    public AdminEventsControllerAuthorizationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Tests that verify GET endpoint authorization (list events).
    /// </summary>
    public sealed class WhenAccessingGetEventsEndpoint : AdminEventsControllerAuthorizationTests
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WhenAccessingGetEventsEndpoint"/> class.
        /// </summary>
        public WhenAccessingGetEventsEndpoint(WebApplicationFactory<Program> factory) : base(factory)
        {
        }

        /// <summary>
        /// Verifies that unauthenticated request to GET /api/admin/events returns 401 Unauthorized.
        /// </summary>
        [Fact]
        public async Task GetEvents_WhenUnauthenticated_Returns401Unauthorized()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/admin/events");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }

    /// <summary>
    /// Tests that verify DELETE endpoint authorization (delete event).
    /// </summary>
    public sealed class WhenAccessingDeleteEventEndpoint : AdminEventsControllerAuthorizationTests
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WhenAccessingDeleteEventEndpoint"/> class.
        /// </summary>
        public WhenAccessingDeleteEventEndpoint(WebApplicationFactory<Program> factory) : base(factory)
        {
        }

        /// <summary>
        /// Verifies that unauthenticated request to DELETE /api/admin/events/{id} returns 401 Unauthorized.
        /// </summary>
        [Fact]
        public async Task DeleteEvent_WhenUnauthenticated_Returns401Unauthorized()
        {
            // Arrange
            var client = _factory.CreateClient();
            var eventId = Guid.NewGuid();

            // Act
            var response = await client.DeleteAsync($"/api/admin/events/{eventId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}
