using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SportsClubEventManager.Web.Services;

namespace SportsClubEventManager.Web.Tests.Services;

/// <summary>
/// Unit tests for the ApiCallLoggingHandler DelegatingHandler, verifying it logs a "calling"
/// entry before sending the request and a "result" entry after receiving the response, including
/// the HTTP method, request URI, resulting status code and elapsed time.
/// </summary>
public sealed class ApiCallLoggingHandlerTests
{
    /// <summary>
    /// A minimal terminal HttpMessageHandler that returns a fixed response after every request,
    /// so tests can control exactly what status code ApiCallLoggingHandler observes.
    /// </summary>
    private sealed class FixedResponseHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(statusCode));
    }

    /// <summary>
    /// Verifies that an Information entry mentioning the HTTP method and request URI is logged
    /// before the request is sent to the inner handler.
    /// </summary>
    [Fact]
    public async Task SendAsync_BeforeSendingRequest_LogsCallingEntryWithMethodAndUri()
    {
        // Arrange
        var logger = Substitute.For<ILogger<ApiCallLoggingHandler>>();
        var handler = new ApiCallLoggingHandler(logger) { InnerHandler = new FixedResponseHandler(HttpStatusCode.OK) };
        using var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/events");

        // Act
        await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        LoggerCallInspector.HasEntry(logger, LogLevel.Information, "Calling Api").Should().BeTrue();
        LoggerCallInspector.HasEntry(logger, LogLevel.Information, "GET").Should().BeTrue();
        LoggerCallInspector.HasEntry(logger, LogLevel.Information, "https://api.example.com/events").Should().BeTrue();
    }

    /// <summary>
    /// Verifies that an Information entry with the resulting status code and elapsed time is
    /// logged after the response is received.
    /// </summary>
    [Fact]
    public async Task SendAsync_AfterReceivingResponse_LogsResultEntryWithStatusCodeAndElapsedTime()
    {
        // Arrange
        var logger = Substitute.For<ILogger<ApiCallLoggingHandler>>();
        var handler = new ApiCallLoggingHandler(logger)
        {
            InnerHandler = new FixedResponseHandler(HttpStatusCode.Created)
        };
        using var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.example.com/events/register");

        // Act
        await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        LoggerCallInspector.HasEntry(logger, LogLevel.Information, "-> 201").Should().BeTrue();
        LoggerCallInspector.HasEntry(logger, LogLevel.Information, "ms").Should().BeTrue();
    }

    /// <summary>
    /// Verifies that the response returned to the caller is the exact one produced by the inner
    /// handler (the logging handler only observes it, never replaces or mutates it).
    /// </summary>
    [Fact]
    public async Task SendAsync_WhenCalled_ReturnsInnerHandlerResponseUnchanged()
    {
        // Arrange
        var logger = Substitute.For<ILogger<ApiCallLoggingHandler>>();
        var handler = new ApiCallLoggingHandler(logger)
        {
            InnerHandler = new FixedResponseHandler(HttpStatusCode.NotFound)
        };
        using var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/events/missing");

        // Act
        var response = await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Verifies that both the "calling" and "result" entries are logged exactly once per request,
    /// with no extra or missing log calls.
    /// </summary>
    [Fact]
    public async Task SendAsync_WhenCalledOnce_LogsExactlyTwoInformationEntries()
    {
        // Arrange
        var logger = Substitute.For<ILogger<ApiCallLoggingHandler>>();
        var handler = new ApiCallLoggingHandler(logger) { InnerHandler = new FixedResponseHandler(HttpStatusCode.OK) };
        using var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/events");

        // Act
        await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        LoggerCallInspector.CountEntries(logger, LogLevel.Information).Should().Be(2);
    }
}

/// <summary>
/// Test-only helper that inspects the calls NSubstitute recorded against the generic
/// <see cref="ILogger.Log{TState}"/> method — the single method every <c>ILogger</c> extension
/// (LogInformation/LogDebug/LogWarning/LogError) ultimately calls. NSubstitute cannot assert
/// directly on this generic method because the actual <c>TState</c> used at the call site
/// (<c>Microsoft.Extensions.Logging.FormattedLogValues</c>) is internal to the framework, so
/// instead this helper walks the recorded calls and compares the already-formatted message text,
/// exactly as it would be rendered by any real logging provider.
/// </summary>
internal static class LoggerCallInspector
{
    /// <summary>
    /// Returns true if the given logger recorded at least one log call at <paramref name="level"/>
    /// whose formatted message contains <paramref name="expectedSubstring"/>.
    /// </summary>
    public static bool HasEntry(ILogger logger, LogLevel level, string expectedSubstring) =>
        logger.ReceivedCalls().Any(call =>
        {
            if (call.GetMethodInfo().Name != nameof(ILogger.Log))
            {
                return false;
            }

            var arguments = call.GetArguments();
            if (arguments.Length < 5 || arguments[0] is not LogLevel callLevel || callLevel != level)
            {
                return false;
            }

            var message = arguments[2]?.ToString() ?? string.Empty;
            return message.Contains(expectedSubstring, StringComparison.Ordinal);
        });

    /// <summary>
    /// Counts how many log calls were recorded at the given <paramref name="level"/>.
    /// </summary>
    public static int CountEntries(ILogger logger, LogLevel level) =>
        logger.ReceivedCalls().Count(call =>
        {
            if (call.GetMethodInfo().Name != nameof(ILogger.Log))
            {
                return false;
            }

            var arguments = call.GetArguments();
            return arguments.Length >= 5 && arguments[0] is LogLevel callLevel && callLevel == level;
        });
}
