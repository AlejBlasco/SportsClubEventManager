namespace SportsClubEventManager.Web.Configuration;

/// <summary>
/// Registers the Web host's strongly typed, startup-validated configuration options.
/// </summary>
public static class WebConfigurationExtensions
{
    /// <summary>
    /// Binds and validates every critical configuration section owned by the Web host
    /// (<see cref="ApiSettingsOptions"/>, <see cref="CookieSettingsOptions"/>). Each section is
    /// validated eagerly during <c>IHost.StartAsync()</c> (via <c>ValidateOnStart()</c>), fixing
    /// the lazy validation gap that previously let the process start successfully even when
    /// <c>ApiSettings:BaseUrl</c> was missing, only to fail later on the first typed
    /// <see cref="HttpClient"/> resolution.
    /// </summary>
    /// <param name="services">The service collection to register the options against.</param>
    /// <param name="configuration">The application configuration to bind sections from.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddWebConfigurationOptions(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ApiSettingsOptions>()
            .Bind(configuration.GetSection(ApiSettingsOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<CookieSettingsOptions>()
            .Bind(configuration.GetSection(CookieSettingsOptions.SectionName))
            .ValidateOnStart();

        return services;
    }
}
