using System.Security.Claims;
using Serilog.Context;

namespace SportsClubEventManager.Api.Middleware;

/// <summary>
/// Pushes the authenticated user's id and role into the ambient <see cref="LogContext"/> so every
/// log entry emitted for the rest of this request is correlated with who made it. Must run after
/// <c>UseAuthentication</c>/<c>UseAuthorization</c>, since it depends on <see cref="HttpContext.User"/>
/// already being populated.
/// </summary>
public sealed class RequestUserLogContextMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Unknown";
            var role = context.User.FindFirstValue(ClaimTypes.Role) ?? "Unknown";

            using (LogContext.PushProperty("UserId", userId))
            using (LogContext.PushProperty("UserRole", role))
            {
                await next(context);
                return;
            }
        }

        await next(context);
    }
}
