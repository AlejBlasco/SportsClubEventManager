using System.Net;
using FluentAssertions;
using NSubstitute;
using SportsClubEventManager.Web.Middleware;
using SportsClubEventManager.Web.Services;

namespace SportsClubEventManager.Web.Tests.Services;

/// <summary>
/// Unit tests for the CorrelationIdHandler DelegatingHandler, verifying that every outgoing
/// request gets the X-Correlation-Id header set to the current circuit's correlation id, as
/// provided by ICorrelationIdProvider.
/// </summary>
public sealed class CorrelationIdHandlerTests
{
    /// <summary>
    /// A minimal terminal HttpMessageHandler that captures the request it receives (after passing
    /// through CorrelationIdHandler) and returns a fixed 200 OK response, so tests can inspect
    /// exactly what the handler under test attached to the outgoing request.
    /// </summary>
    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? CapturedRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    /// <summary>
    /// Verifies that the outgoing request receives an X-Correlation-Id header whose value matches
    /// ICorrelationIdProvider.CorrelationId.
    /// </summary>
    [Fact]
    public async Task SendAsync_WhenCalled_AddsCorrelationIdHeaderFromProvider()
    {
        // Arrange
        var correlationIdProvider = Substitute.For<ICorrelationIdProvider>();
        correlationIdProvider.CorrelationId.Returns("circuit-correlation-id");

        var capturingHandler = new CapturingHandler();
        var handler = new CorrelationIdHandler(correlationIdProvider) { InnerHandler = capturingHandler };
        using var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/events");

        // Act
        await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        capturingHandler.CapturedRequest.Should().NotBeNull();
        capturingHandler.CapturedRequest!.Headers.TryGetValues(CorrelationIdMiddleware.HeaderName, out var values)
            .Should().BeTrue();
        values.Should().ContainSingle().Which.Should().Be("circuit-correlation-id");
    }

    /// <summary>
    /// Verifies that the request is still forwarded to the inner handler (the pipeline is not
    /// short-circuited) and its response is returned unchanged.
    /// </summary>
    [Fact]
    public async Task SendAsync_WhenCalled_ForwardsRequestToInnerHandlerAndReturnsItsResponse()
    {
        // Arrange
        var correlationIdProvider = Substitute.For<ICorrelationIdProvider>();
        correlationIdProvider.CorrelationId.Returns("circuit-correlation-id");

        var capturingHandler = new CapturingHandler();
        var handler = new CorrelationIdHandler(correlationIdProvider) { InnerHandler = capturingHandler };
        using var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.example.com/events/register");

        // Act
        var response = await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        capturingHandler.CapturedRequest.Should().BeSameAs(request);
    }

    /// <summary>
    /// Verifies that the same CorrelationIdHandler instance attaches the same header value across
    /// multiple outgoing requests within the same circuit (the provider is read, not regenerated,
    /// on every call).
    /// </summary>
    [Fact]
    public async Task SendAsync_WhenCalledMultipleTimes_AttachesSameCorrelationIdEachTime()
    {
        // Arrange
        var correlationIdProvider = Substitute.For<ICorrelationIdProvider>();
        correlationIdProvider.CorrelationId.Returns("stable-circuit-id");

        var capturingHandler = new CapturingHandler();
        var handler = new CorrelationIdHandler(correlationIdProvider) { InnerHandler = capturingHandler };
        using var invoker = new HttpMessageInvoker(handler);

        // Act
        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/a"), CancellationToken.None);
        var firstValue = capturingHandler.CapturedRequest!.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single();

        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/b"), CancellationToken.None);
        var secondValue = capturingHandler.CapturedRequest!.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single();

        // Assert
        firstValue.Should().Be("stable-circuit-id");
        secondValue.Should().Be("stable-circuit-id");
    }
}
