using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Application.Common.Models.Notifications;
using SportsClubEventManager.Infrastructure.Configuration;
using SportsClubEventManager.Infrastructure.Notifications;
using Xunit;

namespace SportsClubEventManager.Tests.Infrastructure.Notifications;

/// <summary>
/// Unit tests for <see cref="N8nWorkflowNotifier"/>, the HTTP implementation of
/// <see cref="IWorkflowNotifier"/> that POSTs JSON payloads to n8n's per-workflow webhook URLs.
/// Follows the same "fake terminal HttpMessageHandler behind a substituted IHttpClientFactory"
/// pattern already used by ApiAvailabilityHealthCheckTests (Web project), avoiding the need for a
/// real network call, a running n8n instance, or a new mocking dependency (e.g. WireMock.Net).
/// </summary>
public sealed class N8nWorkflowNotifierTests
{
    private const string HttpClientName = "N8n";

    /// <summary>
    /// A minimal terminal HttpMessageHandler that returns a fixed status code for every request
    /// and records the request it received, so tests can control exactly what
    /// N8nWorkflowNotifier observes and verify what it sent.
    /// </summary>
    private sealed class CapturingHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        public HttpRequestMessage? CapturedRequest { get; private set; }

        /// <summary>
        /// The request body read out eagerly during SendAsync, since N8nWorkflowNotifier disposes
        /// the request (and its JsonContent) via a `using` statement immediately after SendAsync
        /// returns — reading CapturedRequest.Content afterward would throw ObjectDisposedException.
        /// </summary>
        public string? CapturedRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedRequest = request;
            if (request.Content is not null)
            {
                CapturedRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            return new HttpResponseMessage(statusCode);
        }
    }

    /// <summary>
    /// A terminal HttpMessageHandler that always throws the given exception, simulating network
    /// failures such as a connection refusal or a request timeout.
    /// </summary>
    private sealed class ThrowingHandler(Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw exception;
    }

    private static N8nOptions EnabledOptions() => new()
    {
        Enabled = true,
        RegistrationConfirmedWebhookUrl = "https://n8n.example.com/webhook/registration-confirmed",
        EventUpdatedWebhookUrl = "https://n8n.example.com/webhook/event-updated",
        EventCancelledWebhookUrl = "https://n8n.example.com/webhook/event-cancelled",
        EventReminderWebhookUrl = "https://n8n.example.com/webhook/event-reminder",
        WebhookToken = "shared-secret-token",
        TimeoutSeconds = 5
    };

    private static RegistrationConfirmedPayload SamplePayload() => new()
    {
        EventId = Guid.NewGuid(),
        EventTitle = "Basketball Tournament",
        EventDate = DateTime.UtcNow.AddDays(7),
        Location = "Sports Hall A",
        UserEmail = "player@example.com",
        UserName = "Alex Player"
    };

    private static (N8nWorkflowNotifier Notifier, IHttpClientFactory HttpClientFactory, IApplicationMetrics Metrics) CreateSut(
        N8nOptions options, HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient(HttpClientName).Returns(httpClient);

        var metrics = Substitute.For<IApplicationMetrics>();
        var logger = Substitute.For<ILogger<N8nWorkflowNotifier>>();

        var notifier = new N8nWorkflowNotifier(httpClientFactory, Options.Create(options), metrics, logger);

        return (notifier, httpClientFactory, metrics);
    }

    /// <summary>
    /// Tests covering the "integration disabled" short-circuit shared by every Notify* method.
    /// </summary>
    public sealed class WhenIntegrationIsDisabled
    {
        /// <summary>
        /// Verifies that no HTTP client is ever created when Notifications:N8n:Enabled is false,
        /// so a disabled integration never attempts any outbound call (the default in every
        /// environment except production).
        /// </summary>
        [Fact]
        public async Task NotifyRegistrationConfirmedAsync_WhenDisabled_NeverCreatesHttpClient()
        {
            // Arrange
            var options = new N8nOptions { Enabled = false };
            var (notifier, httpClientFactory, metrics) = CreateSut(options, new CapturingHandler(HttpStatusCode.OK));

            // Act
            await notifier.NotifyRegistrationConfirmedAsync(SamplePayload(), CancellationToken.None);

            // Assert
            httpClientFactory.DidNotReceive().CreateClient(Arg.Any<string>());
            metrics.DidNotReceive().RecordWorkflowNotificationSent(Arg.Any<string>(), Arg.Any<bool>());
        }
    }

    /// <summary>
    /// Tests covering that each public Notify* method routes to the correct configured webhook
    /// URL and reports the correct bounded "workflow" label, so a copy/paste mistake between the
    /// four near-identical methods would be caught.
    /// </summary>
    public sealed class WhenRoutingToWebhooks
    {
        /// <summary>
        /// Verifies that NotifyRegistrationConfirmedAsync posts to RegistrationConfirmedWebhookUrl
        /// and records the "registration-confirmed" workflow label.
        /// </summary>
        [Fact]
        public async Task NotifyRegistrationConfirmedAsync_WhenEnabled_PostsToRegistrationConfirmedUrl()
        {
            // Arrange
            var options = EnabledOptions();
            var handler = new CapturingHandler(HttpStatusCode.OK);
            var (notifier, _, metrics) = CreateSut(options, handler);

            // Act
            await notifier.NotifyRegistrationConfirmedAsync(SamplePayload(), CancellationToken.None);

            // Assert
            handler.CapturedRequest.Should().NotBeNull();
            handler.CapturedRequest!.RequestUri.Should().Be(new Uri(options.RegistrationConfirmedWebhookUrl));
            metrics.Received(1).RecordWorkflowNotificationSent("registration-confirmed", true);
        }

        /// <summary>
        /// Verifies that NotifyEventUpdatedAsync posts to EventUpdatedWebhookUrl and records the
        /// "event-updated" workflow label.
        /// </summary>
        [Fact]
        public async Task NotifyEventUpdatedAsync_WhenEnabled_PostsToEventUpdatedUrl()
        {
            // Arrange
            var options = EnabledOptions();
            var handler = new CapturingHandler(HttpStatusCode.OK);
            var (notifier, _, metrics) = CreateSut(options, handler);
            var payload = new EventChangedPayload
            {
                EventId = Guid.NewGuid(),
                EventTitle = "Volleyball Match",
                EventDate = DateTime.UtcNow.AddDays(3),
                Location = "Court B",
                ChangeType = "updated",
                Recipients = []
            };

            // Act
            await notifier.NotifyEventUpdatedAsync(payload, CancellationToken.None);

            // Assert
            handler.CapturedRequest.Should().NotBeNull();
            handler.CapturedRequest!.RequestUri.Should().Be(new Uri(options.EventUpdatedWebhookUrl));
            metrics.Received(1).RecordWorkflowNotificationSent("event-updated", true);
        }

        /// <summary>
        /// Verifies that NotifyEventCancelledAsync posts to EventCancelledWebhookUrl and records
        /// the "event-cancelled" workflow label.
        /// </summary>
        [Fact]
        public async Task NotifyEventCancelledAsync_WhenEnabled_PostsToEventCancelledUrl()
        {
            // Arrange
            var options = EnabledOptions();
            var handler = new CapturingHandler(HttpStatusCode.OK);
            var (notifier, _, metrics) = CreateSut(options, handler);
            var payload = new EventChangedPayload
            {
                EventId = Guid.NewGuid(),
                EventTitle = "Yoga Class",
                EventDate = DateTime.UtcNow.AddDays(2),
                Location = "Studio 1",
                ChangeType = "cancelled",
                Recipients = []
            };

            // Act
            await notifier.NotifyEventCancelledAsync(payload, CancellationToken.None);

            // Assert
            handler.CapturedRequest.Should().NotBeNull();
            handler.CapturedRequest!.RequestUri.Should().Be(new Uri(options.EventCancelledWebhookUrl));
            metrics.Received(1).RecordWorkflowNotificationSent("event-cancelled", true);
        }

        /// <summary>
        /// Verifies that NotifyEventReminderAsync posts to EventReminderWebhookUrl and records the
        /// "event-reminder" workflow label.
        /// </summary>
        [Fact]
        public async Task NotifyEventReminderAsync_WhenEnabled_PostsToEventReminderUrl()
        {
            // Arrange
            var options = EnabledOptions();
            var handler = new CapturingHandler(HttpStatusCode.OK);
            var (notifier, _, metrics) = CreateSut(options, handler);
            var payload = new EventReminderPayload
            {
                EventId = Guid.NewGuid(),
                EventTitle = "Tennis Match",
                EventDate = DateTime.UtcNow.AddHours(24),
                Location = "Court 3",
                IntervalHours = 24,
                Recipients = []
            };

            // Act
            await notifier.NotifyEventReminderAsync(payload, CancellationToken.None);

            // Assert
            handler.CapturedRequest.Should().NotBeNull();
            handler.CapturedRequest!.RequestUri.Should().Be(new Uri(options.EventReminderWebhookUrl));
            metrics.Received(1).RecordWorkflowNotificationSent("event-reminder", true);
        }
    }

    /// <summary>
    /// Tests covering the exact shape of the outgoing HTTP request (method, header, JSON body),
    /// which n8n's imported Webhook Trigger + Header Auth credential depend on.
    /// </summary>
    public sealed class WhenBuildingTheRequest
    {
        /// <summary>
        /// Verifies that the request is a POST.
        /// </summary>
        [Fact]
        public async Task PostAsync_WhenCalled_UsesHttpPostMethod()
        {
            // Arrange
            var handler = new CapturingHandler(HttpStatusCode.OK);
            var (notifier, _, _) = CreateSut(EnabledOptions(), handler);

            // Act
            await notifier.NotifyRegistrationConfirmedAsync(SamplePayload(), CancellationToken.None);

            // Assert
            handler.CapturedRequest!.Method.Should().Be(HttpMethod.Post);
        }

        /// <summary>
        /// Verifies that the configured shared-secret token is sent as the X-N8n-Webhook-Token
        /// header, matching the native n8n Header Auth credential on each imported webhook node.
        /// </summary>
        [Fact]
        public async Task PostAsync_WhenCalled_AddsWebhookTokenHeader()
        {
            // Arrange
            var options = EnabledOptions();
            var handler = new CapturingHandler(HttpStatusCode.OK);
            var (notifier, _, _) = CreateSut(options, handler);

            // Act
            await notifier.NotifyRegistrationConfirmedAsync(SamplePayload(), CancellationToken.None);

            // Assert
            handler.CapturedRequest!.Headers.TryGetValues("X-N8n-Webhook-Token", out var values).Should().BeTrue();
            values.Should().ContainSingle().Which.Should().Be(options.WebhookToken);
        }

        /// <summary>
        /// Verifies that the JSON body sent to n8n matches every field of the payload passed by
        /// the caller, so the workflow's downstream nodes receive the expected data.
        /// </summary>
        [Fact]
        public async Task PostAsync_WhenCalled_SerializesPayloadFieldsIntoJsonBody()
        {
            // Arrange
            var handler = new CapturingHandler(HttpStatusCode.OK);
            var (notifier, _, _) = CreateSut(EnabledOptions(), handler);
            var payload = SamplePayload();

            // Act
            await notifier.NotifyRegistrationConfirmedAsync(payload, CancellationToken.None);

            // Assert
            handler.CapturedRequestBody.Should().NotBeNullOrEmpty();
            var sentPayload = JsonSerializer.Deserialize<RegistrationConfirmedPayload>(
                handler.CapturedRequestBody!,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            sentPayload.Should().NotBeNull();
            sentPayload!.EventId.Should().Be(payload.EventId);
            sentPayload.EventTitle.Should().Be(payload.EventTitle);
            sentPayload.Location.Should().Be(payload.Location);
            sentPayload.UserEmail.Should().Be(payload.UserEmail);
            sentPayload.UserName.Should().Be(payload.UserName);
        }
    }

    /// <summary>
    /// Tests covering failure handling: every public method must never throw, per the contract
    /// documented on <see cref="IWorkflowNotifier"/>, regardless of what n8n or the network do.
    /// </summary>
    public sealed class WhenTheOutboundCallFails
    {
        /// <summary>
        /// Verifies that a non-2xx response from n8n is recorded as a failed notification and
        /// does not throw, so the calling command handler's business operation is unaffected.
        /// </summary>
        [Fact]
        public async Task NotifyRegistrationConfirmedAsync_WhenN8nRespondsWithServerError_RecordsFailureAndDoesNotThrow()
        {
            // Arrange
            var handler = new CapturingHandler(HttpStatusCode.InternalServerError);
            var (notifier, _, metrics) = CreateSut(EnabledOptions(), handler);

            // Act
            var act = async () => await notifier.NotifyRegistrationConfirmedAsync(SamplePayload(), CancellationToken.None);

            // Assert
            await act.Should().NotThrowAsync();
            metrics.Received(1).RecordWorkflowNotificationSent("registration-confirmed", false);
        }

        /// <summary>
        /// Verifies that a network-level failure (DNS resolution failure, connection refused —
        /// surfaced by HttpClient as HttpRequestException) is swallowed, recorded as a failed
        /// notification, and never rethrown.
        /// </summary>
        [Fact]
        public async Task NotifyRegistrationConfirmedAsync_WhenConnectionFails_RecordsFailureAndDoesNotThrow()
        {
            // Arrange
            var handler = new ThrowingHandler(new HttpRequestException("Connection refused."));
            var (notifier, _, metrics) = CreateSut(EnabledOptions(), handler);

            // Act
            var act = async () => await notifier.NotifyRegistrationConfirmedAsync(SamplePayload(), CancellationToken.None);

            // Assert
            await act.Should().NotThrowAsync();
            metrics.Received(1).RecordWorkflowNotificationSent("registration-confirmed", false);
        }

        /// <summary>
        /// Verifies that a timeout (surfaced by HttpClient as TaskCanceledException once the
        /// configured per-request timeout elapses) is swallowed, recorded as a failed
        /// notification, and never rethrown.
        /// </summary>
        [Fact]
        public async Task NotifyRegistrationConfirmedAsync_WhenRequestTimesOut_RecordsFailureAndDoesNotThrow()
        {
            // Arrange
            var handler = new ThrowingHandler(new TaskCanceledException("The request timed out."));
            var (notifier, _, metrics) = CreateSut(EnabledOptions(), handler);

            // Act
            var act = async () => await notifier.NotifyRegistrationConfirmedAsync(SamplePayload(), CancellationToken.None);

            // Assert
            await act.Should().NotThrowAsync();
            metrics.Received(1).RecordWorkflowNotificationSent("registration-confirmed", false);
        }

        /// <summary>
        /// Verifies that an exception type outside the documented HttpRequestException/
        /// TaskCanceledException pair (e.g. a genuinely unexpected bug) is NOT swallowed, so a
        /// regression that widens the catch clause too far would fail this test rather than
        /// silently hiding a real defect.
        /// </summary>
        [Fact]
        public async Task NotifyRegistrationConfirmedAsync_WhenAnUnexpectedExceptionTypeIsThrown_PropagatesIt()
        {
            // Arrange
            var handler = new ThrowingHandler(new InvalidOperationException("Unexpected failure."));
            var (notifier, _, _) = CreateSut(EnabledOptions(), handler);

            // Act
            var act = async () => await notifier.NotifyRegistrationConfirmedAsync(SamplePayload(), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>();
        }
    }
}
