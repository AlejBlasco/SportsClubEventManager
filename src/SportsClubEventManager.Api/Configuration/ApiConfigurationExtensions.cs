using Microsoft.Extensions.Options;

namespace SportsClubEventManager.Api.Configuration;

/// <summary>
/// Registers the API host's strongly typed, startup-validated configuration options.
/// </summary>
public static class ApiConfigurationExtensions
{
    /// <summary>
    /// Binds and validates every critical configuration section owned by the API host
    /// (<see cref="JwtSettingsOptions"/>, <see cref="GoogleAuthOptions"/>,
    /// <see cref="AdminUserOptions"/>, <see cref="CorsOptions"/>). Each section is validated
    /// eagerly during <c>IHost.StartAsync()</c> (via <c>ValidateOnStart()</c>), so the process
    /// fails fast — before serving any request — with every validation error aggregated into
    /// a single exception.
    /// </summary>
    /// <param name="services">The service collection to register the options against.</param>
    /// <param name="configuration">The application configuration to bind sections from.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddApiConfigurationOptions(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtSettingsOptions>()
            .Bind(configuration.GetSection(JwtSettingsOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<GoogleAuthOptions>()
            .Bind(configuration.GetSection(GoogleAuthOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<AdminUserOptions>()
            .Bind(configuration.GetSection(AdminUserOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<AdminUserOptions>, AdminUserOptionsValidator>();

        services.AddOptions<CorsOptions>()
            .Bind(configuration.GetSection(CorsOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<CorsOptions>, CorsOptionsValidator>();

        return services;
    }
}
