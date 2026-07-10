using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using SportsClubEventManager.Api.Configuration;
using Xunit;

namespace SportsClubEventManager.Api.Tests.Configuration;

/// <summary>
/// Unit tests for <see cref="ApiConfigurationExtensions.AddApiConfigurationOptions"/>, verifying
/// that it binds <see cref="JwtSettingsOptions"/>, <see cref="GoogleAuthOptions"/>,
/// <see cref="AdminUserOptions"/> and <see cref="CorsOptions"/> from configuration, and that
/// resolving <see cref="IOptions{TOptions}"/> triggers the validators configured for each section
/// (the same validation <c>ValidateOnStart()</c> forces eagerly at host startup) without requiring
/// a full <c>WebApplicationFactory</c>-driven host.
/// </summary>
public sealed class ApiConfigurationExtensionsTests
{
    private static IConfiguration CreateValidConfiguration()
    {
        var settings = new Dictionary<string, string?>
        {
            ["Authentication:JwtSettings:SecretKey"] = "a-valid-secret-key-of-at-least-32-chars",
            ["Authentication:JwtSettings:Issuer"] = "SportsClubEventManager.Api",
            ["Authentication:JwtSettings:Audience"] = "SportsClubEventManager.Web",
            ["Authentication:Google:ClientId"] = "a-google-client-id",
            ["Authentication:Google:ClientSecret"] = "a-google-client-secret"
        };

        return new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
    }

    private static IServiceProvider BuildProvider(IConfiguration configuration, string environmentName)
    {
        var services = new ServiceCollection();
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(environmentName);
        services.AddSingleton(environment);

        services.AddApiConfigurationOptions(configuration);

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Verifies that AddApiConfigurationOptions returns the same service collection for chaining.
    /// </summary>
    [Fact]
    public void AddApiConfigurationOptions_ReturnsServiceCollectionForChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateValidConfiguration();

        // Act
        var result = services.AddApiConfigurationOptions(configuration);

        // Assert
        result.Should().BeSameAs(services);
    }

    /// <summary>
    /// Verifies that valid configuration binds JwtSettingsOptions correctly and resolves without
    /// throwing, exercising the [Required, MinLength(32)] data annotations on SecretKey.
    /// </summary>
    [Fact]
    public void AddApiConfigurationOptions_WithValidConfiguration_BindsJwtSettingsOptions()
    {
        // Arrange
        var configuration = CreateValidConfiguration();
        var provider = BuildProvider(configuration, Environments.Development);

        // Act
        var jwtSettings = provider.GetRequiredService<IOptions<JwtSettingsOptions>>().Value;

        // Assert
        jwtSettings.Issuer.Should().Be("SportsClubEventManager.Api");
        jwtSettings.Audience.Should().Be("SportsClubEventManager.Web");
    }

    /// <summary>
    /// Verifies that resolving JwtSettingsOptions throws an OptionsValidationException when
    /// SecretKey is shorter than the required 32 characters, matching the same failure ValidateOnStart
    /// would raise eagerly at host startup.
    /// </summary>
    [Fact]
    public void AddApiConfigurationOptions_WithShortSecretKey_ThrowsOptionsValidationExceptionOnResolve()
    {
        // Arrange
        var settings = new Dictionary<string, string?>
        {
            ["Authentication:JwtSettings:SecretKey"] = "too-short",
            ["Authentication:JwtSettings:Issuer"] = "SportsClubEventManager.Api",
            ["Authentication:JwtSettings:Audience"] = "SportsClubEventManager.Web",
            ["Authentication:Google:ClientId"] = "a-google-client-id",
            ["Authentication:Google:ClientSecret"] = "a-google-client-secret"
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var provider = BuildProvider(configuration, Environments.Development);

        // Act
        var act = () => provider.GetRequiredService<IOptions<JwtSettingsOptions>>().Value;

        // Assert
        act.Should().Throw<OptionsValidationException>().WithMessage("*SecretKey*");
    }

    /// <summary>
    /// Verifies that valid configuration binds GoogleAuthOptions correctly, including the
    /// CallbackPath default value when not explicitly configured.
    /// </summary>
    [Fact]
    public void AddApiConfigurationOptions_WithValidConfiguration_BindsGoogleAuthOptions()
    {
        // Arrange
        var configuration = CreateValidConfiguration();
        var provider = BuildProvider(configuration, Environments.Development);

        // Act
        var googleAuth = provider.GetRequiredService<IOptions<GoogleAuthOptions>>().Value;

        // Assert
        googleAuth.ClientId.Should().Be("a-google-client-id");
        googleAuth.ClientSecret.Should().Be("a-google-client-secret");
        googleAuth.CallbackPath.Should().Be("/signin-google");
    }

    /// <summary>
    /// Verifies that resolving AdminUserOptions in Development succeeds even without a configured
    /// password, exercising the custom AdminUserOptionsValidator wired up as IValidateOptions.
    /// </summary>
    [Fact]
    public void AddApiConfigurationOptions_InDevelopmentWithNoAdminPassword_ResolvesSuccessfully()
    {
        // Arrange
        var configuration = CreateValidConfiguration();
        var provider = BuildProvider(configuration, Environments.Development);

        // Act
        var act = () => provider.GetRequiredService<IOptions<AdminUserOptions>>().Value;

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that resolving CorsOptions outside Development throws an OptionsValidationException
    /// when no allowed origins are configured, exercising the custom CorsOptionsValidator wired up
    /// as IValidateOptions.
    /// </summary>
    [Fact]
    public void AddApiConfigurationOptions_OutsideDevelopmentWithNoCorsOrigins_ThrowsOnResolve()
    {
        // Arrange
        var configuration = CreateValidConfiguration();
        var provider = BuildProvider(configuration, Environments.Production);

        // Act
        var act = () => provider.GetRequiredService<IOptions<CorsOptions>>().Value;

        // Assert
        act.Should().Throw<OptionsValidationException>().WithMessage("*Cors:AllowedOrigins*");
    }
}
