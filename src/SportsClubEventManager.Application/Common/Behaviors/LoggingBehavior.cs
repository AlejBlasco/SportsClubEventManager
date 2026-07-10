using System.Diagnostics;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace SportsClubEventManager.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that logs structured, uniform entries around every command/query
/// handled by the application: an "handling" entry (Information) before invoking the handler, the
/// request payload itself (Debug only, never the response, to keep log size bounded), a "handled"
/// entry with the elapsed time on success, and the failure (with elapsed time) before rethrowing —
/// validation failures are logged at Warning, any other exception at Error. Never swallows an
/// exception; <c>ExceptionHandlingMiddleware</c> keeps full responsibility for translating the
/// exception into an HTTP response.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <summary>
    /// Logs before and after invoking the next handler in the pipeline, re-throwing any exception
    /// after logging it.
    /// </summary>
    /// <param name="request">The request object.</param>
    /// <param name="next">The next handler in the pipeline.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response from the handler.</returns>
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        logger.LogInformation("Handling {RequestName}", requestName);
        logger.LogDebug("Handling {RequestName} {@Request}", requestName, request); // Debug only

        try
        {
            var response = await next();
            logger.LogInformation(
                "Handled {RequestName} in {ElapsedMilliseconds}ms", requestName, stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex) when (ex is not ValidationException) // ValidationException logged as Warning below
        {
            logger.LogError(
                ex, "Handling {RequestName} failed after {ElapsedMilliseconds}ms", requestName, stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (ValidationException ex)
        {
            logger.LogWarning(
                "Handling {RequestName} failed validation after {ElapsedMilliseconds}ms: {ValidationErrors}",
                requestName, stopwatch.ElapsedMilliseconds, ex.Errors.Select(e => e.ErrorMessage));
            throw;
        }
    }
}
