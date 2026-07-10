using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using SportsClubEventManager.Web.Configuration;

namespace SportsClubEventManager.Web.Tests.Configuration;

/// <summary>
/// Unit tests for <see cref="ApiSettingsOptions"/>, verifying the DataAnnotations
/// (<c>[Required, Url]</c>) applied to <see cref="ApiSettingsOptions.BaseUrl"/> that
/// <c>AddWebConfigurationOptions</c> enforces via <c>ValidateDataAnnotations().ValidateOnStart()</c>.
/// </summary>
public sealed class ApiSettingsOptionsTests
{
    private static IList<ValidationResult> ValidateOptions(ApiSettingsOptions options)
    {
        var validationContext = new ValidationContext(options);
        var validationResults = new List<ValidationResult>();
        Validator.TryValidateObject(options, validationContext, validationResults, validateAllProperties: true);
        return validationResults;
    }

    /// <summary>
    /// Verifies that an empty BaseUrl fails validation because of the [Required] attribute.
    /// </summary>
    [Fact]
    public void Validate_WithEmptyBaseUrl_FailsRequiredValidation()
    {
        // Arrange
        var options = new ApiSettingsOptions { BaseUrl = string.Empty };

        // Act
        var results = ValidateOptions(options);

        // Assert
        results.Should().Contain(r => r.MemberNames.Contains(nameof(ApiSettingsOptions.BaseUrl)));
    }

    /// <summary>
    /// Verifies that a non-URL string fails validation because of the [Url] attribute, even
    /// though it satisfies [Required].
    /// </summary>
    [Fact]
    public void Validate_WithNonUrlBaseUrl_FailsUrlValidation()
    {
        // Arrange
        var options = new ApiSettingsOptions { BaseUrl = "not-a-valid-url" };

        // Act
        var results = ValidateOptions(options);

        // Assert
        results.Should().Contain(r => r.MemberNames.Contains(nameof(ApiSettingsOptions.BaseUrl)));
    }

    /// <summary>
    /// Verifies that whitespace-only BaseUrl fails validation, since [Required] rejects blank
    /// strings, not only null/empty ones.
    /// </summary>
    [Fact]
    public void Validate_WithWhitespaceBaseUrl_FailsRequiredValidation()
    {
        // Arrange
        var options = new ApiSettingsOptions { BaseUrl = "   " };

        // Act
        var results = ValidateOptions(options);

        // Assert
        results.Should().Contain(r => r.MemberNames.Contains(nameof(ApiSettingsOptions.BaseUrl)));
    }

    /// <summary>
    /// Verifies that a well-formed absolute URL passes both [Required] and [Url] validation.
    /// </summary>
    [Fact]
    public void Validate_WithValidAbsoluteUrl_Succeeds()
    {
        // Arrange
        var options = new ApiSettingsOptions { BaseUrl = "https://api.sportsclub.example.com" };

        // Act
        var results = ValidateOptions(options);

        // Assert
        results.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that the default parameterless value of BaseUrl (empty string) fails validation,
    /// guarding against a caller that forgets to bind the section at all.
    /// </summary>
    [Fact]
    public void Validate_WithDefaultConstructedOptions_Fails()
    {
        // Arrange
        var options = new ApiSettingsOptions();

        // Act
        var results = ValidateOptions(options);

        // Assert
        results.Should().NotBeEmpty();
    }
}
