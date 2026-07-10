using FluentAssertions;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using SportsClubEventManager.Api.Configuration;
using Xunit;

namespace SportsClubEventManager.Api.Tests.Configuration;

/// <summary>
/// Unit tests for <see cref="AdminUserOptionsValidator"/>, covering the environment-conditional
/// rule that makes <see cref="AdminUserOptions.Password"/> optional in Development (where a
/// well-known local seed password is acceptable) but mandatory in every other environment.
/// </summary>
public sealed class AdminUserOptionsValidatorTests
{
    private static IHostEnvironment CreateEnvironment(string environmentName)
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(environmentName);
        return environment;
    }

    /// <summary>
    /// Verifies that validation succeeds in Development even when no password is configured,
    /// preserving the local development workflow that seeds the admin account without a secret.
    /// </summary>
    [Fact]
    public void Validate_InDevelopmentWithNoPassword_Succeeds()
    {
        // Arrange
        var environment = CreateEnvironment(Environments.Development);
        var validator = new AdminUserOptionsValidator(environment);
        var options = new AdminUserOptions { Password = null };

        // Act
        var result = validator.Validate(name: null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that validation succeeds in Development even when the password is an empty string
    /// (not just null), since both represent "no password configured".
    /// </summary>
    [Fact]
    public void Validate_InDevelopmentWithBlankPassword_Succeeds()
    {
        // Arrange
        var environment = CreateEnvironment(Environments.Development);
        var validator = new AdminUserOptionsValidator(environment);
        var options = new AdminUserOptions { Password = "   " };

        // Act
        var result = validator.Validate(name: null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that validation fails outside Development when no password is configured at all,
    /// since a missing admin password must never be silently accepted in a real deployment.
    /// </summary>
    [Fact]
    public void Validate_OutsideDevelopmentWithNoPassword_Fails()
    {
        // Arrange
        var environment = CreateEnvironment(Environments.Production);
        var validator = new AdminUserOptionsValidator(environment);
        var options = new AdminUserOptions { Password = null };

        // Act
        var result = validator.Validate(name: null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("AdminUser:Password");
    }

    /// <summary>
    /// Verifies that validation fails outside Development when the configured password is
    /// whitespace-only, treating it the same as a missing value.
    /// </summary>
    [Fact]
    public void Validate_OutsideDevelopmentWithBlankPassword_Fails()
    {
        // Arrange
        var environment = CreateEnvironment(Environments.Staging);
        var validator = new AdminUserOptionsValidator(environment);
        var options = new AdminUserOptions { Password = "   " };

        // Act
        var result = validator.Validate(name: null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("AdminUser:Password");
    }

    /// <summary>
    /// Verifies that validation succeeds outside Development once a real, non-blank password is
    /// configured, confirming the rule only rejects missing/blank values, not the environment itself.
    /// </summary>
    [Fact]
    public void Validate_OutsideDevelopmentWithNonBlankPassword_Succeeds()
    {
        // Arrange
        var environment = CreateEnvironment(Environments.Production);
        var validator = new AdminUserOptionsValidator(environment);
        var options = new AdminUserOptions { Password = "a-real-secret-value" };

        // Act
        var result = validator.Validate(name: null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }
}
