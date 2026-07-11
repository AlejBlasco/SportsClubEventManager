using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Infrastructure.Authentication;
using SportsClubEventManager.Infrastructure.Authentication.OAuth2;
using SportsClubEventManager.Infrastructure.Common;
using SportsClubEventManager.Infrastructure.Import;
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
