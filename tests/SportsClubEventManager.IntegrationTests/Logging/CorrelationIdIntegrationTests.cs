using System.Linq;
using System.Net.Http;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SportsClubEventManager.Api.Middleware;
using SportsClubEventManager.Infrastructure.Persistence;

namespace SportsClubEventManager.IntegrationTests.Logging;

/// <summary>
/// End-to-end tests for CorrelationIdMiddleware, verifying that a X-Correlation-Id sent on a
/// request is echoed back on the response header (or generated when absent), through the real
/// ASP.NET Core pipeline via WebApplicationFactory.
/// </summary>
/// <remarks>
/// Per the design's own risk note, this scenario is intentionally scoped to the header contract
/// only (not an in-memory Serilog sink capturing the emitted LogEvent's CorrelationId property):
/// building a custom Serilog sink-capture mechanism just for this one assertion would be
/// disproportionate infrastructure for what the header-echo behavior alone already proves — the
/// middleware pushes the same value into LogContext.PushProperty before it can possibly reach the
/// response header (see CorrelationIdMiddleware.InvokeAsync), so a correct header round-trip is
/// strong evidence the ambient LogContext enrichment ran as well. Requests target the anonymous
/// "GET /" endpoint mapped in Api/Program.cs (no authentication/authorization required), so these
/// tests exercise only CorrelationIdMiddleware itself, not RequestUserLogContextMiddleware.
/// As with every other test in this project, this class is not currently wired into CI
/// (tests/SportsClubEventManager.IntegrationTests is not referenced by .github/workflows/ci.yml,
/// a pre-existing gap unrelated to this issue).
/// </remarks>
public sealed class CorrelationIdIntegrationTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="CorrelationIdIntegrationTests"/> class.
    /// </summary>
    /// <param name="fixture">The database fixture providing SQL Server container access.</param>
    public CorrelationIdIntegrationTests(DatabaseFixture fixture)
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
    /// Verifies that a X-Correlation-Id header sent on the request is echoed back unchanged on
    /// the response.
    /// </summary>
    [Fact]
    public async Task Get_WhenRequestSendsCorrelationIdHeader_EchoesSameValueOnResponse()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add(CorrelationIdMiddleware.HeaderName, "my-test-correlation-id");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.Headers.TryGetValues(CorrelationIdMiddleware.HeaderName, out var values).Should().BeTrue();
        values.Should().ContainSingle().Which.Should().Be("my-test-correlation-id");
    }

    /// <summary>
    /// Verifies that a new X-Correlation-Id (a valid GUID) is generated and returned on the
    /// response when the request does not send one.
    /// </summary>
    [Fact]
    public async Task Get_WhenRequestDoesNotSendCorrelationIdHeader_GeneratesAndReturnsNewGuid()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.Headers.TryGetValues(CorrelationIdMiddleware.HeaderName, out var values).Should().BeTrue();
        var correlationId = values.Should().ContainSingle().Subject;
        Guid.TryParse(correlationId, out _).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that two separate requests without an incoming correlation id each receive a
    /// distinct generated value, i.e. the id is not accidentally reused/cached across requests.
    /// </summary>
    [Fact]
    public async Task Get_WhenCalledTwiceWithoutHeader_ReturnsDifferentCorrelationIdsEachTime()
    {
        // Arrange
        var firstRequest = new HttpRequestMessage(HttpMethod.Get, "/");
        var secondRequest = new HttpRequestMessage(HttpMethod.Get, "/");

        // Act
        var firstResponse = await _client.SendAsync(firstRequest);
        var secondResponse = await _client.SendAsync(secondRequest);

        // Assert
        firstResponse.Headers.TryGetValues(CorrelationIdMiddleware.HeaderName, out var firstValues);
        secondResponse.Headers.TryGetValues(CorrelationIdMiddleware.HeaderName, out var secondValues);

        firstValues!.Single().Should().NotBe(secondValues!.Single());
    }
}
