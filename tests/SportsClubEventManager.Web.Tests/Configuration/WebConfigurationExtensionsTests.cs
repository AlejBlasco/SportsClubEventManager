using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SportsClubEventManager.Web.Configuration;

namespace SportsClubEventManager.Web.Tests.Configuration;

/// <summary>
/// Unit tests for <see cref="WebConfigurationExtensions.AddWebConfigurationOptions"/>, verifying
/// that it binds both <see cref="ApiSettingsOptions"/> and <see cref="CookieSettingsOptions"/> from
/// configuration, and that resolving <see cref="IOptions{TOptions}"/> triggers the
/// DataAnnotations validation configured for <see cref="ApiSettingsOptions"/> even without an
/// actual host startup (resolving <c>IOptions&lt;T&gt;.Value</c> runs the same validators that
/// <c>ValidateOnStart()</c> forces eagerly).
/// </summary>
public sealed class WebConfigurationExtensionsTests
{
    private static IConfiguration CreateConfiguration(string? baseUrl)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Authentication:CookieSettings:CookieName"] = ".Test.Auth",
            ["Authentication:CookieSettings:LoginPath"] = "/custom-login",
            ["Authentication:CookieSettings:ExpireTimeSpan"] = "01:00:00",
            ["Authentication:CookieSettings:SlidingExpiration"] = "false"
        };

        if (baseUrl is not null)
        {
            settings["ApiSettings:BaseUrl"] = baseUrl;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
    }

    /// <summary>
    /// Verifies that AddWebConfigurationOptions returns the same service collection for chaining.
    /// </summary>
    [Fact]
    public void AddWebConfigurationOptions_ReturnsServiceCollectionForChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration("https://api.sportsclub.example.com");

        // Act
        var result = services.AddWebConfigurationOptions(configuration);

        // Assert
        result.Should().BeSameAs(services);
    }

    /// <summary>
    /// Verifies that a valid ApiSettings:BaseUrl binds correctly and resolves without throwing.
    /// </summary>
    [Fact]
    public void AddWebConfigurationOptions_WithValidBaseUrl_BindsApiSettingsOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration("https://api.sportsclub.example.com");
        services.AddWebConfigurationOptions(configuration);
        var provider = services.BuildServiceProvider();

        // Act
        var apiSettings = provider.GetRequiredService<IOptions<ApiSettingsOptions>>().Value;

        // Assert
        apiSettings.BaseUrl.Should().Be("https://api.sportsclub.example.com");
    }

    /// <summary>
    /// Verifies that resolving ApiSettingsOptions throws an OptionsValidationException when
    /// ApiSettings:BaseUrl is missing, proving the [Required, Url] data annotations are enforced
    /// (the same validation ValidateOnStart forces eagerly at host startup).
    /// </summary>
    [Fact]
    public void AddWebConfigurationOptions_WithMissingBaseUrl_ThrowsOptionsValidationExceptionOnResolve()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(baseUrl: null);
        services.AddWebConfigurationOptions(configuration);
        var provider = services.BuildServiceProvider();

        // Act
        var act = () => provider.GetRequiredService<IOptions<ApiSettingsOptions>>().Value;

        // Assert
        act.Should().Throw<OptionsValidationException>()
            .WithMessage("*BaseUrl*");
    }

    /// <summary>
    /// Verifies that CookieSettingsOptions binds every configured value, including automatic
    /// string-to-TimeSpan conversion of ExpireTimeSpan by the native configuration binder.
    /// </summary>
    [Fact]
    public void AddWebConfigurationOptions_BindsCookieSettingsOptionsFromConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration("https://api.sportsclub.example.com");
        services.AddWebConfigurationOptions(configuration);
        var provider = services.BuildServiceProvider();

        // Act
        var cookieSettings = provider.GetRequiredService<IOptions<CookieSettingsOptions>>().Value;

        // Assert
        cookieSettings.CookieName.Should().Be(".Test.Auth");
        cookieSettings.LoginPath.Should().Be("/custom-login");
        cookieSettings.ExpireTimeSpan.Should().Be(TimeSpan.FromHours(1));
        cookieSettings.SlidingExpiration.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that CookieSettingsOptions falls back to its documented defaults when no
    /// configuration values are supplied for it at all.
    /// </summary>
    [Fact]
    public void AddWebConfigurationOptions_WithNoCookieSettingsConfigured_UsesDefaults()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiSettings:BaseUrl"] = "https://api.sportsclub.example.com"
            })
            .Build();
        services.AddWebConfigurationOptions(configuration);
        var provider = services.BuildServiceProvider();

        // Act
        var cookieSettings = provider.GetRequiredService<IOptions<CookieSettingsOptions>>().Value;

        // Assert
        cookieSettings.CookieName.Should().Be(".SportsClubEventManager.Auth");
        cookieSettings.LoginPath.Should().Be("/login");
        cookieSettings.SlidingExpiration.Should().BeTrue();
    }
}
