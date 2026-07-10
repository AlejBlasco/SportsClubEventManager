using FluentAssertions;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using SportsClubEventManager.Api.Configuration;
using Xunit;

namespace SportsClubEventManager.Api.Tests.Configuration;

/// <summary>
/// Unit tests for <see cref="CorsOptionsValidator"/>, covering the environment-conditional rule
/// that allows an empty allowed-origins list in Development (where the CORS policy falls back to
/// localhost) but requires at least one non-blank origin in every other environment.
/// </summary>
public sealed class CorsOptionsValidatorTests
{
    private static IHostEnvironment CreateEnvironment(string environmentName)
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(environmentName);
        return environment;
    }

    /// <summary>
    /// Verifies that validation succeeds in Development even when no allowed origins are
    /// configured, preserving the local development fallback to localhost.
    /// </summary>
    [Fact]
    public void Validate_InDevelopmentWithEmptyOrigins_Succeeds()
    {
        // Arrange
        var environment = CreateEnvironment(Environments.Development);
        var validator = new CorsOptionsValidator(environment);
        var options = new CorsOptions { AllowedOrigins = [] };

        // Act
        var result = validator.Validate(name: null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that validation fails outside Development when the allowed-origins array is
    /// completely empty, preventing CORS from silently accepting no configured origin in production.
    /// </summary>
    [Fact]
    public void Validate_OutsideDevelopmentWithEmptyOrigins_Fails()
    {
        // Arrange
        var environment = CreateEnvironment(Environments.Production);
        var validator = new CorsOptionsValidator(environment);
        var options = new CorsOptions { AllowedOrigins = [] };

        // Act
        var result = validator.Validate(name: null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("Cors:AllowedOrigins");
    }

    /// <summary>
    /// Verifies that validation fails outside Development when the allowed-origins array
    /// contains only blank/whitespace entries, which must be treated the same as no origin at all.
    /// </summary>
    [Fact]
    public void Validate_OutsideDevelopmentWithBlankOnlyOrigins_Fails()
    {
        // Arrange
        var environment = CreateEnvironment(Environments.Staging);
        var validator = new CorsOptionsValidator(environment);
        var options = new CorsOptions { AllowedOrigins = ["   ", ""] };

        // Act
        var result = validator.Validate(name: null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("Cors:AllowedOrigins");
    }

    /// <summary>
    /// Verifies that validation succeeds outside Development once at least one non-blank origin
    /// is configured, even if other entries in the array are blank.
    /// </summary>
    [Fact]
    public void Validate_OutsideDevelopmentWithAtLeastOneNonBlankOrigin_Succeeds()
    {
        // Arrange
        var environment = CreateEnvironment(Environments.Production);
        var validator = new CorsOptionsValidator(environment);
        var options = new CorsOptions { AllowedOrigins = ["   ", "https://sportsclub.example.com"] };

        // Act
        var result = validator.Validate(name: null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }
}
