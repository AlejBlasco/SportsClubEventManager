using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using SportsClubEventManager.Web.HealthChecks;

namespace SportsClubEventManager.Web.Tests.HealthChecks;

/// <summary>
/// Unit tests for <see cref="ApiAvailabilityHealthCheck"/>, verifying that it reports the Api's
/// reachability by calling its <c>health/live</c> endpoint via the "HealthCheckApiClient" named
/// <see cref="HttpClient"/>: a 200 response reports Healthy, any other status code or a network
/// exception (timeout, DNS failure, connection refused) reports Unhealthy.
/// </summary>
public sealed class ApiAvailabilityHealthCheckTests
{
    private const string HttpClientName = "HealthCheckApiClient";

    /// <summary>
    /// A minimal terminal HttpMessageHandler that returns a fixed status code for every request
    /// and records the request it received, so tests can control exactly what
    /// ApiAvailabilityHealthCheck observes and verify what it requested.
    /// </summary>
    private sealed class FixedResponseHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        public HttpRequestMessage? CapturedRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedRequest = request;
            return Task.FromResult(new HttpResponseMessage(statusCode));
        }
    }

    /// <summary>
    /// A terminal HttpMessageHandler that always throws the given exception, simulating network
    /// failures such as timeouts, DNS resolution failures or connection refusals.
    /// </summary>
    private sealed class ThrowingHandler(Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw exception;
    }

    private static IHttpClientFactory CreateHttpClientFactory(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.example.com/")
        };

        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient(HttpClientName).Returns(httpClient);

        return httpClientFactory;
    }

    /// <summary>
    /// Verifies that a 200 OK response from the Api's liveness endpoint is reported as Healthy.
    /// </summary>
    [Fact]
    public async Task CheckHealthAsync_WhenApiRespondsWith200_ReturnsHealthy()
    {
        // Arrange
        var handler = new FixedResponseHandler(HttpStatusCode.OK);
        var httpClientFactory = CreateHttpClientFactory(handler);
        var healthCheck = new ApiAvailabilityHealthCheck(httpClientFactory);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
    }

    /// <summary>
    /// Verifies that a 500 Internal Server Error response is reported as Unhealthy, with the
    /// status code included in the result description.
    /// </summary>
    [Fact]
    public async Task CheckHealthAsync_WhenApiRespondsWith500_ReturnsUnhealthyWithStatusCodeInDescription()
    {
        // Arrange
        var handler = new FixedResponseHandler(HttpStatusCode.InternalServerError);
        var httpClientFactory = CreateHttpClientFactory(handler);
        var healthCheck = new ApiAvailabilityHealthCheck(httpClientFactory);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("500");
    }

    /// <summary>
    /// Verifies that a 503 Service Unavailable response (the Api reporting its own unhealthy
    /// state) is reported as Unhealthy by the Web host's check as well.
    /// </summary>
    [Fact]
    public async Task CheckHealthAsync_WhenApiRespondsWith503_ReturnsUnhealthy()
    {
        // Arrange
        var handler = new FixedResponseHandler(HttpStatusCode.ServiceUnavailable);
        var httpClientFactory = CreateHttpClientFactory(handler);
        var healthCheck = new ApiAvailabilityHealthCheck(httpClientFactory);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("503");
    }

    /// <summary>
    /// Verifies that a timeout while calling the Api (surfaced by HttpClient as a
    /// TaskCanceledException once its configured Timeout elapses) is reported as Unhealthy with
    /// the triggering exception attached to the result.
    /// </summary>
    [Fact]
    public async Task CheckHealthAsync_WhenRequestTimesOut_ReturnsUnhealthyWithException()
    {
        // Arrange
        var timeoutException = new TaskCanceledException("The request timed out.");
        var handler = new ThrowingHandler(timeoutException);
        var httpClientFactory = CreateHttpClientFactory(handler);
        var healthCheck = new ApiAvailabilityHealthCheck(httpClientFactory);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Exception.Should().BeSameAs(timeoutException);
    }

    /// <summary>
    /// Verifies that a network-level failure (DNS resolution failure or connection refused,
    /// surfaced by HttpClient as an HttpRequestException) is reported as Unhealthy with the
    /// triggering exception attached to the result.
    /// </summary>
    [Fact]
    public async Task CheckHealthAsync_WhenConnectionFails_ReturnsUnhealthyWithException()
    {
        // Arrange
        var connectionException = new HttpRequestException("Connection refused.");
        var handler = new ThrowingHandler(connectionException);
        var httpClientFactory = CreateHttpClientFactory(handler);
        var healthCheck = new ApiAvailabilityHealthCheck(httpClientFactory);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Exception.Should().BeSameAs(connectionException);
    }

    /// <summary>
    /// Verifies that the health check resolves the dedicated "HealthCheckApiClient" named
    /// HttpClient from the factory, rather than the default/unnamed client.
    /// </summary>
    [Fact]
    public async Task CheckHealthAsync_WhenCalled_ResolvesDedicatedNamedHttpClient()
    {
        // Arrange
        var handler = new FixedResponseHandler(HttpStatusCode.OK);
        var httpClientFactory = CreateHttpClientFactory(handler);
        var healthCheck = new ApiAvailabilityHealthCheck(httpClientFactory);

        // Act
        await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        // Assert
        httpClientFactory.Received(1).CreateClient(HttpClientName);
    }

    /// <summary>
    /// Verifies that the health check issues a GET request against the "health/live" relative
    /// path, not "health" or "health/ready" (avoiding cascading readiness checks, see design doc).
    /// </summary>
    [Fact]
    public async Task CheckHealthAsync_WhenCalled_RequestsHealthLiveEndpoint()
    {
        // Arrange
        var handler = new FixedResponseHandler(HttpStatusCode.OK);
        var httpClientFactory = CreateHttpClientFactory(handler);
        var healthCheck = new ApiAvailabilityHealthCheck(httpClientFactory);

        // Act
        await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        // Assert
        handler.CapturedRequest.Should().NotBeNull();
        handler.CapturedRequest!.Method.Should().Be(HttpMethod.Get);
        handler.CapturedRequest.RequestUri.Should().Be(new Uri("https://api.example.com/health/live"));
    }
}
