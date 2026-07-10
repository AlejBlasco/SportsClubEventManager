using Serilog.Context;

namespace SportsClubEventManager.Web.Middleware;

/// <summary>
/// Reads the <c>X-Correlation-Id</c> header from the incoming request (generating a new one if
/// absent), pushes it into the ambient <see cref="LogContext"/> so every log entry emitted while
/// handling this request carries it, and echoes it back on the response. Deliberately duplicated
/// from <c>SportsClubEventManager.Api.Middleware.CorrelationIdMiddleware</c> rather than shared,
/// consistent with this repository's convention of keeping small, host-specific middleware
/// classes independent instead of forcing them into the Shared project.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    /// <summary>
    /// The name of the HTTP header used to carry the correlation id between Web, Api and any
    /// external caller.
    /// </summary>
    public const string HeaderName = "X-Correlation-Id";

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var existing)
            ? existing.ToString()
            : Guid.NewGuid().ToString();

        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }
}
