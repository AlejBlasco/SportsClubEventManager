using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace SportsClubEventManager.Api.HealthChecks;

/// <summary>
/// Serializes a <see cref="HealthReport"/> produced by the ASP.NET Core Health Checks
/// middleware into the structured JSON contract exposed by the <c>/health*</c> endpoints
/// (issue #41).
/// </summary>
public static class HealthCheckResponseWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Writes the given <see cref="HealthReport"/> as JSON to the HTTP response.
    /// Outside the Development environment, per-check <c>description</c> values are
    /// truncated to a generic message to avoid leaking dependency details (e.g. database
    /// server names embedded in connection exception messages).
    /// </summary>
    /// <param name="context">The HTTP context for the current request.</param>
    /// <param name="report">The health report produced by the health checks middleware.</param>
    /// <returns>A task representing the asynchronous write operation.</returns>
    public static async Task WriteAsync(HttpContext context, HealthReport report)
    {
        var environment = context.RequestServices.GetRequiredService<IHostEnvironment>();

        context.Response.ContentType = "application/json";

        var response = new
        {
            Status = report.Status.ToString(),
            TotalDurationMs = report.TotalDuration.TotalMilliseconds,
            Checks = report.Entries.Select(entry => new
            {
                Name = entry.Key,
                Status = entry.Value.Status.ToString(),
                Description = FormatDescription(entry.Value, environment),
                DurationMs = entry.Value.Duration.TotalMilliseconds,
                Tags = entry.Value.Tags
            })
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, SerializerOptions));
    }

    private static string? FormatDescription(HealthReportEntry entry, IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            return entry.Description ?? entry.Exception?.Message;
        }

        return entry.Status == HealthStatus.Healthy ? entry.Description : "A dependency check failed.";
    }
}
