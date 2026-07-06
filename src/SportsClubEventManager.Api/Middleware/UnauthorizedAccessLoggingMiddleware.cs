using System.Security.Claims;

namespace SportsClubEventManager.Api.Middleware;

/// <summary>
/// Middleware that logs unauthorized access attempts (403 Forbidden responses) for security auditing.
/// </summary>
public sealed class UnauthorizedAccessLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UnauthorizedAccessLoggingMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnauthorizedAccessLoggingMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger for recording unauthorized access attempts.</param>
    public UnauthorizedAccessLoggingMiddleware(RequestDelegate next, ILogger<UnauthorizedAccessLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware to intercept and log 403 Forbidden responses.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        if (context.Response.StatusCode == StatusCodes.Status403Forbidden)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Anonymous";
            var email = context.User.FindFirstValue(ClaimTypes.Email) ?? "N/A";
            var role = context.User.FindFirstValue(ClaimTypes.Role) ?? "N/A";
            var path = context.Request.Path;
            var method = context.Request.Method;

            _logger.LogWarning(
                "Unauthorized access attempt: User={UserId} Email={Email} Role={Role} attempted {Method} {Path}",
                userId,
                email,
                role,
                method,
                path);
        }
    }
}
