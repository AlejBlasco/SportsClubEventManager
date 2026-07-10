using SportsClubEventManager.Web.Middleware;

namespace SportsClubEventManager.Web.Services;

/// <summary>
/// Attaches the current circuit's correlation id (see <see cref="ICorrelationIdProvider"/>) to
/// outgoing requests made by the Web app's typed HttpClients, so a single circuit's Api calls can
/// be correlated together server-side.
/// </summary>
public sealed class CorrelationIdHandler(ICorrelationIdProvider correlationIdProvider) : DelegatingHandler
{
    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.TryAddWithoutValidation(CorrelationIdMiddleware.HeaderName, correlationIdProvider.CorrelationId);
        return base.SendAsync(request, cancellationToken);
    }
}
