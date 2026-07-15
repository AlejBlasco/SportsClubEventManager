using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Infrastructure.Authentication;
using SportsClubEventManager.Infrastructure.Authentication.OAuth2;
using SportsClubEventManager.Infrastructure.Common;
using SportsClubEventManager.Infrastructure.Configuration;
using SportsClubEventManager.Infrastructure.Import;
using SportsClubEventManager.Infrastructure.Metrics;
using SportsClubEventManager.Infrastructure.Notifications;
using SportsClubEventManager.Infrastructure.Persistence;
using SportsClubEventManager.Infrastructure.Services;

namespace SportsClubEventManager.Infrastructure;

/// <summary>
/// Dependency injection configuration for Infrastructure layer.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds Infrastructure layer services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is not configured. " +
                "Set it via User Secrets (local) or the CONNECTION_STRING environment variable (Docker).");
        }

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(
                connectionString,
                sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                });
        });

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<AppDbContext>());

        // Registers a health check that verifies connectivity to the database via AppDbContext
        // (issue #41). Tagged "ready" so it is included in readiness probes but not liveness
        // probes. AddHealthChecks() can be called again by each host's Program.cs to register
        // additional checks (e.g. Web's ApiAvailabilityHealthCheck); the framework accumulates
        // registrations instead of overwriting them.
        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>(name: "database", tags: ["ready"]);

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<GoogleOAuth2Handler>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<ICsvEventImportParser, CsvEventImportParser>();

        // Hands off Google OAuth2 tokens from the Api's callback to Web (issue #125). Singleton
        // because it wraps IMemoryCache, itself a process-wide singleton, and holds no per-request
        // state of its own.
        services.AddMemoryCache();
        services.AddSingleton<IOAuthExchangeCodeStore, OAuthExchangeCodeStore>();

        // Binds and validates the "Metrics" configuration section (issue #42), consumed by
        // ActiveEventsGaugeUpdater below to make its refresh interval configurable instead of
        // hardcoded, following the same ValidateOnStart() pattern as the Api/Web host options.
        services.AddOptions<MetricsOptions>()
            .Bind(configuration.GetSection(MetricsOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Business metrics (issue #42): IApplicationMetrics is a singleton because the
        // prometheus-net Counter/Gauge instances it wraps must be created once per process and
        // reused; ActiveEventsGaugeUpdater is a hosted service that periodically recomputes the
        // "active events" gauge, resolving IApplicationDbContext through its own scope.
        services.AddSingleton<IApplicationMetrics, ApplicationMetrics>();
        services.AddHostedService<ActiveEventsGaugeUpdater>();

        // Binds and validates the "Notifications:N8n" configuration section (issue #37). Validation
        // is conditional on Enabled (see N8nOptionsValidator) because no project-owned n8n instance
        // exists outside production — the section is legitimately empty everywhere else.
        services.AddOptions<N8nOptions>()
            .Bind(configuration.GetSection(N8nOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<N8nOptions>, N8nOptionsValidator>();

        services.AddHttpClient("N8n");

        // Outbound n8n workflow notifications (issue #37): IWorkflowNotifier is invoked from the
        // affected command handlers after a successful SaveChangesAsync/CommitAsync, same pattern
        // as IApplicationMetrics/IAuditService above. EventReminderBackgroundService is the
        // BackgroundService that polls for events entering a configured reminder window.
        services.AddScoped<IWorkflowNotifier, N8nWorkflowNotifier>();
        services.AddHostedService<EventReminderBackgroundService>();

        return services;
    }

    /// <summary>
    /// Applies any pending Entity Framework Core migrations to the database.
    /// </summary>
    /// <param name="serviceProvider">The application service provider.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task MigrateDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (dbContext.Database.IsRelational())
            await dbContext.Database.MigrateAsync();
    }
}
