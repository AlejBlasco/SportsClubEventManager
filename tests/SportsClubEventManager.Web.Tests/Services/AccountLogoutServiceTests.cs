using System.Net;
using FluentAssertions;
using SportsClubEventManager.Web.Services;

namespace SportsClubEventManager.Web.Tests.Services;

/// <summary>
/// Unit tests for the AccountLogoutService class.
/// </summary>
public sealed class AccountLogoutServiceTests
{
    /// <summary>
    /// Tests that LogoutAsync completes without throwing when the API returns 204 No Content.
    /// </summary>
    [Fact]
    public async Task LogoutAsync_WhenApiReturns204_CompletesSuccessfully()
    {
        // Arrange
        var httpClient = CreateHttpClientWithResponse(HttpStatusCode.NoContent);
        var service = new AccountLogoutService(httpClient);

        // Act
        var act = async () => await service.LogoutAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Tests that LogoutAsync throws HttpRequestException when the API returns 401 Unauthorized
    /// (e.g. the access_token attached by AuthTokenHandler has already expired).
    /// </summary>
    [Fact]
    public async Task LogoutAsync_WhenApiReturns401_ThrowsHttpRequestException()
    {
        // Arrange
        var httpClient = CreateHttpClientWithResponse(HttpStatusCode.Unauthorized);
        var service = new AccountLogoutService(httpClient);

        // Act
        var act = async () => await service.LogoutAsync();

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    /// <summary>
    /// Tests that LogoutAsync throws HttpRequestException when the API returns 500 Server Error.
    /// </summary>
    [Fact]
    public async Task LogoutAsync_WhenApiReturns500_ThrowsHttpRequestException()
    {
        // Arrange
        var httpClient = CreateHttpClientWithResponse(HttpStatusCode.InternalServerError);
        var service = new AccountLogoutService(httpClient);

        // Act
        var act = async () => await service.LogoutAsync();

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    /// <summary>
    /// Tests that LogoutAsync respects cancellation token.
    /// </summary>
    [Fact]
    public async Task LogoutAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var httpClient = CreateHttpClientWithDelay();
        var service = new AccountLogoutService(httpClient);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = async () => await service.LogoutAsync(cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private static HttpClient CreateHttpClientWithResponse(HttpStatusCode statusCode)
    {
        var handler = new TestHttpMessageHandler(statusCode);
        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost:7001")
        };
    }

    private static HttpClient CreateHttpClientWithDelay()
    {
        var handler = new DelayHttpMessageHandler();
        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost:7001")
        };
    }

    private sealed class TestHttpMessageHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(statusCode));
        }
    }

    private sealed class DelayHttpMessageHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        }
    }
}
