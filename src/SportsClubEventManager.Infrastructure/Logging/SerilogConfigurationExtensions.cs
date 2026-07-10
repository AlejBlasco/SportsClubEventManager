using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Formatting.Compact;

namespace SportsClubEventManager.Infrastructure.Logging;

/// <summary>
/// Extension methods that wire up Serilog as the logging provider for an ASP.NET Core host.
/// Centralized here so both hosts (Api, Web) share the exact same sinks, enrichers and
/// sensitive-data redaction policy instead of duplicating the bootstrap in each <c>Program.cs</c>.
/// </summary>
public static class SerilogConfigurationExtensions
{
    /// <summary>
    /// Configures Serilog as the logging provider for <paramref name="hostBuilder"/>. Reads the
    /// <c>"Serilog"</c> configuration section (minimum levels and namespace overrides), enriches
    /// every log event with the ambient <see cref="Serilog.Context.LogContext"/> properties, the
    /// machine name, the environment name and the given <paramref name="applicationName"/>,
    /// applies the <see cref="SensitiveValueEnricher"/> redaction policy, and writes structured
    /// JSON to both the console and a daily rolling file (retained for 7 days).
    /// </summary>
    /// <param name="hostBuilder">The host builder being configured.</param>
    /// <param name="applicationName">
    /// The logical application name (e.g. <c>"SportsClubEventManager.Api"</c>) attached to every
    /// log event emitted by this host, so entries from both hosts can be told apart once
    /// aggregated by a centralized logging system.
    /// </param>
    /// <returns>The host builder, for chaining.</returns>
    public static IHostBuilder AddSerilogLogging(this IHostBuilder hostBuilder, string applicationName)
    {
        return hostBuilder.UseSerilog((context, services, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentName()
                .Enrich.WithProperty("Application", applicationName)
                .Enrich.With<SensitiveValueEnricher>()
                .WriteTo.Console(new CompactJsonFormatter())
                .WriteTo.File(
                    new CompactJsonFormatter(),
                    path: "logs/log-.json",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7);
        });
    }
}
