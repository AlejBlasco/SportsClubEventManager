using Microsoft.Extensions.Options;

namespace SportsClubEventManager.Infrastructure.Configuration;

/// <summary>
/// Validates <see cref="N8nOptions"/> only when the integration is enabled — mirrors the
/// conditional-validation pattern already used by AdminUserOptionsValidator/CorsOptionsValidator
/// (Api/Configuration), adapted here because the whole section is legitimately empty whenever
/// Notifications:N8n:Enabled is false (the default in every environment except production).
/// </summary>
public sealed class N8nOptionsValidator : IValidateOptions<N8nOptions>
{
    /// <summary>
    /// Validates the bound <see cref="N8nOptions"/>, requiring the webhook URLs and shared token
    /// only when the integration is enabled.
    /// </summary>
    /// <param name="name">The name of the options instance being validated.</param>
    /// <param name="options">The options instance to validate.</param>
    /// <returns>A successful result, or a failure listing the missing required values.</returns>
    public ValidateOptionsResult Validate(string? name, N8nOptions options)
    {
        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(options.RegistrationConfirmedWebhookUrl)) missing.Add(nameof(options.RegistrationConfirmedWebhookUrl));
        if (string.IsNullOrWhiteSpace(options.EventUpdatedWebhookUrl)) missing.Add(nameof(options.EventUpdatedWebhookUrl));
        if (string.IsNullOrWhiteSpace(options.EventCancelledWebhookUrl)) missing.Add(nameof(options.EventCancelledWebhookUrl));
        if (string.IsNullOrWhiteSpace(options.EventReminderWebhookUrl)) missing.Add(nameof(options.EventReminderWebhookUrl));
        if (string.IsNullOrWhiteSpace(options.WebhookToken)) missing.Add(nameof(options.WebhookToken));

        return missing.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(
                $"Notifications:N8n:Enabled is true but the following required values are missing: {string.Join(", ", missing)}.");
    }
}
