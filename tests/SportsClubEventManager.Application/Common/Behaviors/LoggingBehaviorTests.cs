using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SportsClubEventManager.Application.Common.Behaviors;
using Xunit;

namespace SportsClubEventManager.Application.Tests.Common.Behaviors;

/// <summary>
/// A minimal MediatR request used to exercise <see cref="LoggingBehavior{TRequest,TResponse}"/>
/// without depending on any real command/query from the Application layer. Carries a marker
/// string so tests can assert the request payload does (or does not) show up in a given log
/// entry. Declared at namespace level (not nested/private) because NSubstitute needs to generate
/// a dynamic proxy for <c>ILogger&lt;LoggingBehavior&lt;TestRequest, TestResponse&gt;&gt;</c>,
/// which requires both generic arguments to be at least as visible as the substituted type.
/// </summary>
public sealed record TestRequest(string RequestMarker) : IRequest<TestResponse>;

/// <summary>
/// A minimal response type carrying its own marker string, distinct from
/// <see cref="TestRequest.RequestMarker"/>, so tests can assert the response is never logged.
/// See <see cref="TestRequest"/> for why this is declared at namespace level.
/// </summary>
public sealed record TestResponse(string ResponseMarker);

/// <summary>
/// Tests for LoggingBehavior to verify structured logging around the MediatR pipeline: an
/// "handling" entry before the handler runs, an "handled" entry with elapsed time on success,
/// exception propagation (never swallowed), and the Warning-vs-Error split between
/// <see cref="ValidationException"/> and any other exception.
/// </summary>
public class LoggingBehaviorTests
{
    /// <summary>
    /// Tests that verify logging behavior when the wrapped handler completes successfully.
    /// </summary>
    public sealed class WhenHandlerSucceeds : LoggingBehaviorTests
    {
        /// <summary>
        /// Verifies that an Information "Handling" entry is logged before the handler runs and an
        /// Information "Handled" entry (with elapsed time) is logged after it completes.
        /// </summary>
        [Fact]
        public async Task Handle_WhenHandlerSucceeds_LogsHandlingBeforeAndHandledAfter()
        {
            // Arrange
            var logger = Substitute.For<ILogger<LoggingBehavior<TestRequest, TestResponse>>>();
            var behavior = new LoggingBehavior<TestRequest, TestResponse>(logger);
            var request = new TestRequest("request-marker");
            RequestHandlerDelegate<TestResponse> next = _ => Task.FromResult(new TestResponse("response-marker"));

            // Act
            var result = await behavior.Handle(request, next, CancellationToken.None);

            // Assert
            result.ResponseMarker.Should().Be("response-marker");
            LoggerTestHelper.HasEntry(logger, LogLevel.Information, "Handling TestRequest").Should().BeTrue();
            LoggerTestHelper.HasEntry(logger, LogLevel.Information, "Handled TestRequest").Should().BeTrue();
        }

        /// <summary>
        /// Verifies that the handler's response is returned unchanged by the behavior.
        /// </summary>
        [Fact]
        public async Task Handle_WhenHandlerSucceeds_ReturnsHandlerResponse()
        {
            // Arrange
            var logger = Substitute.For<ILogger<LoggingBehavior<TestRequest, TestResponse>>>();
            var behavior = new LoggingBehavior<TestRequest, TestResponse>(logger);
            var request = new TestRequest("request-marker");
            var expectedResponse = new TestResponse("expected-response");
            RequestHandlerDelegate<TestResponse> next = _ => Task.FromResult(expectedResponse);

            // Act
            var result = await behavior.Handle(request, next, CancellationToken.None);

            // Assert
            result.Should().BeSameAs(expectedResponse);
        }

        /// <summary>
        /// Verifies that the request payload is logged only at Debug level, and that the response
        /// object is never logged at any level (only the request is destructured, per design).
        /// </summary>
        [Fact]
        public async Task Handle_WhenHandlerSucceeds_LogsOnlyRequestAtDebugAndNeverLogsResponse()
        {
            // Arrange
            var logger = Substitute.For<ILogger<LoggingBehavior<TestRequest, TestResponse>>>();
            var behavior = new LoggingBehavior<TestRequest, TestResponse>(logger);
            var request = new TestRequest("REQUEST_MARKER_XYZ");
            RequestHandlerDelegate<TestResponse> next = _ => Task.FromResult(new TestResponse("RESPONSE_MARKER_XYZ"));

            // Act
            await behavior.Handle(request, next, CancellationToken.None);

            // Assert
            LoggerTestHelper.HasEntry(logger, LogLevel.Debug, "REQUEST_MARKER_XYZ").Should().BeTrue();
            LoggerTestHelper.AnyEntryContains(logger, "RESPONSE_MARKER_XYZ").Should().BeFalse();
        }

        /// <summary>
        /// Verifies that the request is never logged above Debug level (e.g. not duplicated into
        /// the Information "Handling" entry), keeping the Information line free of the full payload.
        /// </summary>
        [Fact]
        public async Task Handle_WhenHandlerSucceeds_DoesNotLogRequestPayloadAtInformationLevel()
        {
            // Arrange
            var logger = Substitute.For<ILogger<LoggingBehavior<TestRequest, TestResponse>>>();
            var behavior = new LoggingBehavior<TestRequest, TestResponse>(logger);
            var request = new TestRequest("ONLY_IN_DEBUG_MARKER");
            RequestHandlerDelegate<TestResponse> next = _ => Task.FromResult(new TestResponse("irrelevant"));

            // Act
            await behavior.Handle(request, next, CancellationToken.None);

            // Assert
            LoggerTestHelper.HasEntry(logger, LogLevel.Information, "ONLY_IN_DEBUG_MARKER").Should().BeFalse();
        }
    }

    /// <summary>
    /// Tests that verify logging and exception propagation when the wrapped handler throws.
    /// </summary>
    public sealed class WhenHandlerFails : LoggingBehaviorTests
    {
        /// <summary>
        /// Verifies that a non-validation exception thrown by the handler is logged at Error level
        /// and then rethrown unchanged (never swallowed).
        /// </summary>
        [Fact]
        public async Task Handle_WhenHandlerThrowsGenericException_LogsErrorAndRethrows()
        {
            // Arrange
            var logger = Substitute.For<ILogger<LoggingBehavior<TestRequest, TestResponse>>>();
            var behavior = new LoggingBehavior<TestRequest, TestResponse>(logger);
            var request = new TestRequest("request-marker");
            var thrown = new InvalidOperationException("handler exploded");
            RequestHandlerDelegate<TestResponse> next = _ => throw thrown;

            // Act
            var act = async () => await behavior.Handle(request, next, CancellationToken.None);

            // Assert
            var assertedException = await act.Should().ThrowAsync<InvalidOperationException>();
            assertedException.Which.Should().BeSameAs(thrown);
            LoggerTestHelper.HasEntry(logger, LogLevel.Error, "Handling TestRequest", expectedException: thrown)
                .Should().BeTrue();
            LoggerTestHelper.HasEntry(logger, LogLevel.Warning, "Handling TestRequest").Should().BeFalse();
        }

        /// <summary>
        /// Verifies that a <see cref="ValidationException"/> thrown by the handler is logged at
        /// Warning level (not Error) and then rethrown unchanged.
        /// </summary>
        [Fact]
        public async Task Handle_WhenHandlerThrowsValidationException_LogsWarningNotErrorAndRethrows()
        {
            // Arrange
            var logger = Substitute.For<ILogger<LoggingBehavior<TestRequest, TestResponse>>>();
            var behavior = new LoggingBehavior<TestRequest, TestResponse>(logger);
            var request = new TestRequest("request-marker");
            var validationFailure = new ValidationFailure("Property", "Property is required");
            var thrown = new ValidationException(new[] { validationFailure });
            RequestHandlerDelegate<TestResponse> next = _ => throw thrown;

            // Act
            var act = async () => await behavior.Handle(request, next, CancellationToken.None);

            // Assert
            var assertedException = await act.Should().ThrowAsync<ValidationException>();
            assertedException.Which.Should().BeSameAs(thrown);
            LoggerTestHelper.HasEntry(logger, LogLevel.Warning, "Handling TestRequest").Should().BeTrue();
            LoggerTestHelper.HasEntry(logger, LogLevel.Error, "Handling TestRequest").Should().BeFalse();
        }

        /// <summary>
        /// Verifies that the handler is still invoked (the exception originates from within it,
        /// not from the behavior itself) before the exception is logged and rethrown.
        /// </summary>
        [Fact]
        public async Task Handle_WhenHandlerThrows_StillLogsHandlingEntryBeforeFailure()
        {
            // Arrange
            var logger = Substitute.For<ILogger<LoggingBehavior<TestRequest, TestResponse>>>();
            var behavior = new LoggingBehavior<TestRequest, TestResponse>(logger);
            var request = new TestRequest("request-marker");
            RequestHandlerDelegate<TestResponse> next = _ => throw new InvalidOperationException("boom");

            // Act
            var act = async () => await behavior.Handle(request, next, CancellationToken.None);
            await act.Should().ThrowAsync<InvalidOperationException>();

            // Assert
            LoggerTestHelper.HasEntry(logger, LogLevel.Information, "Handling TestRequest").Should().BeTrue();
        }
    }
}

/// <summary>
/// Test-only helper that inspects the calls NSubstitute recorded against the generic
/// <see cref="ILogger.Log{TState}"/> method — the single method every <c>ILogger</c> extension
/// (LogInformation/LogDebug/LogWarning/LogError) ultimately calls. NSubstitute cannot assert
/// directly on this generic method because the actual <c>TState</c> used at the call site
/// (<c>Microsoft.Extensions.Logging.FormattedLogValues</c>) is internal to the framework, so
/// instead this helper walks <see cref="NSubstitute.SubstituteExtensions.ReceivedCalls"/> and
/// compares the already-formatted message text, exactly as it would be rendered by any real
/// logging provider.
/// </summary>
internal static class LoggerTestHelper
{
    /// <summary>
    /// Returns true if the given logger recorded at least one log call at <paramref name="level"/>
    /// whose formatted message contains <paramref name="expectedSubstring"/>, optionally requiring
    /// a specific exception instance to have been passed alongside it.
    /// </summary>
    public static bool HasEntry(
        ILogger logger,
        LogLevel level,
        string expectedSubstring,
        Exception? expectedException = null)
    {
        return logger.ReceivedCalls().Any(call =>
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

            if (expectedException is not null && !ReferenceEquals(arguments[3], expectedException))
            {
                return false;
            }

            var message = arguments[2]?.ToString() ?? string.Empty;
            return message.Contains(expectedSubstring, StringComparison.Ordinal);
        });
    }

    /// <summary>
    /// Returns true if the given logger recorded at least one log call, at any level, whose
    /// formatted message contains <paramref name="expectedSubstring"/>.
    /// </summary>
    public static bool AnyEntryContains(ILogger logger, string expectedSubstring)
    {
        return logger.ReceivedCalls().Any(call =>
        {
            if (call.GetMethodInfo().Name != nameof(ILogger.Log))
            {
                return false;
            }

            var arguments = call.GetArguments();
            if (arguments.Length < 5)
            {
                return false;
            }

            var message = arguments[2]?.ToString() ?? string.Empty;
            return message.Contains(expectedSubstring, StringComparison.Ordinal);
        });
    }
}
