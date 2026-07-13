using FluentAssertions;
using SportsClubEventManager.Infrastructure.Configuration;
using Xunit;

namespace SportsClubEventManager.Tests.Infrastructure.Configuration;

/// <summary>
/// Unit tests for <see cref="N8nOptionsValidator"/>, covering the conditional validation rule
/// that only requires the n8n webhook URLs and shared token when the integration is enabled —
/// the whole "Notifications:N8n" section is legitimately empty in every environment except
/// production (issue #37).
/// </summary>
public sealed class N8nOptionsValidatorTests
{
    private readonly N8nOptionsValidator _validator = new();

    /// <summary>
    /// Verifies that validation succeeds when the integration is disabled, even though none of
    /// the webhook URLs or the shared token are configured.
    /// </summary>
    [Fact]
    public void Validate_WhenDisabledWithNoValuesConfigured_Succeeds()
    {
        // Arrange
        var options = new N8nOptions { Enabled = false };

        // Act
        var result = _validator.Validate(name: null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that validation succeeds when the integration is enabled and every required
    /// value (four webhook URLs and the shared token) is configured.
    /// </summary>
    [Fact]
    public void Validate_WhenEnabledWithAllRequiredValuesConfigured_Succeeds()
    {
        // Arrange
        var options = new N8nOptions
        {
            Enabled = true,
            RegistrationConfirmedWebhookUrl = "https://n8n.example.com/webhook/registration-confirmed",
            EventUpdatedWebhookUrl = "https://n8n.example.com/webhook/event-updated",
            EventCancelledWebhookUrl = "https://n8n.example.com/webhook/event-cancelled",
            EventReminderWebhookUrl = "https://n8n.example.com/webhook/event-reminder",
            WebhookToken = "shared-secret-token"
        };

        // Act
        var result = _validator.Validate(name: null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that validation fails when the integration is enabled but every required value
    /// is missing, and that the failure message names every missing property.
    /// </summary>
    [Fact]
    public void Validate_WhenEnabledWithNoValuesConfigured_FailsListingAllMissingValues()
    {
        // Arrange
        var options = new N8nOptions { Enabled = true };

        // Act
        var result = _validator.Validate(name: null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain(nameof(N8nOptions.RegistrationConfirmedWebhookUrl));
        result.FailureMessage.Should().Contain(nameof(N8nOptions.EventUpdatedWebhookUrl));
        result.FailureMessage.Should().Contain(nameof(N8nOptions.EventCancelledWebhookUrl));
        result.FailureMessage.Should().Contain(nameof(N8nOptions.EventReminderWebhookUrl));
        result.FailureMessage.Should().Contain(nameof(N8nOptions.WebhookToken));
    }

    /// <summary>
    /// Verifies that validation fails when the integration is enabled and only the shared
    /// webhook token is missing, while every webhook URL is configured, confirming each required
    /// value is checked independently of the others.
    /// </summary>
    [Fact]
    public void Validate_WhenEnabledWithOnlyWebhookTokenMissing_FailsNamingOnlyWebhookToken()
    {
        // Arrange
        var options = new N8nOptions
        {
            Enabled = true,
            RegistrationConfirmedWebhookUrl = "https://n8n.example.com/webhook/registration-confirmed",
            EventUpdatedWebhookUrl = "https://n8n.example.com/webhook/event-updated",
            EventCancelledWebhookUrl = "https://n8n.example.com/webhook/event-cancelled",
            EventReminderWebhookUrl = "https://n8n.example.com/webhook/event-reminder",
            WebhookToken = string.Empty
        };

        // Act
        var result = _validator.Validate(name: null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain(nameof(N8nOptions.WebhookToken));
        result.FailureMessage.Should().NotContain(nameof(N8nOptions.RegistrationConfirmedWebhookUrl));
        result.FailureMessage.Should().NotContain(nameof(N8nOptions.EventUpdatedWebhookUrl));
        result.FailureMessage.Should().NotContain(nameof(N8nOptions.EventCancelledWebhookUrl));
        result.FailureMessage.Should().NotContain(nameof(N8nOptions.EventReminderWebhookUrl));
    }

    /// <summary>
    /// Verifies that a whitespace-only value is treated the same as a missing value, for every
    /// required string property, matching the same "blank counts as missing" rule already used
    /// by AdminUserOptionsValidator/CorsOptionsValidator.
    /// </summary>
    [Fact]
    public void Validate_WhenEnabledWithWhitespaceOnlyValues_TreatsThemAsMissing()
    {
        // Arrange
        var options = new N8nOptions
        {
            Enabled = true,
            RegistrationConfirmedWebhookUrl = "   ",
            EventUpdatedWebhookUrl = "   ",
            EventCancelledWebhookUrl = "   ",
            EventReminderWebhookUrl = "   ",
            WebhookToken = "   "
        };

        // Act
        var result = _validator.Validate(name: null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain(nameof(N8nOptions.RegistrationConfirmedWebhookUrl));
        result.FailureMessage.Should().Contain(nameof(N8nOptions.WebhookToken));
    }
}
