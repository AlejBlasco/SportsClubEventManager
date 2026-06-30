using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SportsClubEventManager.Application.Common.Interfaces;
using SportsClubEventManager.Infrastructure.Authentication;
using SportsClubEventManager.Infrastructure.Authentication.OAuth2;
using SportsClubEventManager.Infrastructure.Common;
using SportsClubEventManager.Infrastructure.Persistence;

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
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
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

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<GoogleOAuth2Handler>();

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
