using System.Diagnostics;

namespace SportsClubEventManager.Web.Services;

/// <summary>
/// Logs a structured entry before and after every outgoing call made by the Web app's typed
/// HttpClients, recording the HTTP method, request URI, resulting status code and elapsed time.
/// </summary>
public sealed class ApiCallLoggingHandler(ILogger<ApiCallLoggingHandler> logger) : DelegatingHandler
{
    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        logger.LogInformation("Calling Api {Method} {RequestUri}", request.Method, request.RequestUri);

        var response = await base.SendAsync(request, cancellationToken);

        logger.LogInformation(
            "Api call {Method} {RequestUri} -> {StatusCode} in {ElapsedMilliseconds}ms",
            request.Method, request.RequestUri, (int)response.StatusCode, stopwatch.ElapsedMilliseconds);

        return response;
    }
}
