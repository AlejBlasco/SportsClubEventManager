namespace SportsClubEventManager.Web.Services;

/// <summary>
/// Default implementation of <see cref="ICorrelationIdProvider"/>. Registered as Scoped, so
/// Blazor Server creates one instance per DI scope — which, for interactive components, means one
/// instance per circuit (the long-lived SignalR connection backing a user's session), giving the
/// id the same lifetime as the interactive session itself. Deliberately not read from
/// <see cref="IHttpContextAccessor"/>: Microsoft's own documentation warns that
/// <c>HttpContext</c> is not reliable inside interactive Blazor Server components beyond the
/// initial static render, since the circuit is not backed by a single discrete HTTP request.
/// </summary>
public sealed class CorrelationIdProvider : ICorrelationIdProvider
{
    /// <inheritdoc />
    public string CorrelationId { get; } = Guid.NewGuid().ToString();
}
