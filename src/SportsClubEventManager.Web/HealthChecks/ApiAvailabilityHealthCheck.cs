using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SportsClubEventManager.Web.HealthChecks;

/// <summary>
/// Health check that verifies the Api host is reachable, by calling its <c>/health/live</c>
/// endpoint (issue #41). This is the Web host's interpretation of "external API availability":
/// every typed <c>*ManagementService</c> in this Blazor Server app depends on the Api at
/// runtime, so its reachability is a genuine readiness dependency for Web.
/// </summary>
/// <param name="httpClientFactory">
/// Factory used to resolve the dedicated "HealthCheckApiClient" named <see cref="HttpClient"/>,
/// which is configured with <c>ApiSettings:BaseUrl</c> and a short timeout, and deliberately does
/// not attach <c>AuthTokenHandler</c> (the Api's liveness probe is anonymous).
/// </param>
public sealed class ApiAvailabilityHealthCheck(IHttpClientFactory httpClientFactory) : IHealthCheck
{
    private const string HttpClientName = "HealthCheckApiClient";

    /// <summary>
    /// Calls the Api's <c>health/live</c> endpoint and reports <see cref="HealthCheckResult.Healthy"/>
    /// when it responds with HTTP 200. Any other status code, or an exception raised while making
    /// the request (timeout, DNS failure, connection refused), is reported as
    /// <see cref="HealthCheckResult.Unhealthy(string?, System.Exception?, System.Collections.Generic.IReadOnlyDictionary{string, object}?)"/>.
    /// </summary>
    /// <param name="context">The health check execution context.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The outcome of probing the Api's liveness endpoint.</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient(HttpClientName);

        try
        {
            var response = await client.GetAsync("health/live", cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("The Api is reachable.")
                : HealthCheckResult.Unhealthy($"The Api responded with status code {(int)response.StatusCode}.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("The Api could not be reached.", exception);
        }
    }
}
