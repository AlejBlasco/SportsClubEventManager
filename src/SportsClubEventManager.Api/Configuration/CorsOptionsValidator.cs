using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace SportsClubEventManager.Api.Configuration;

/// <summary>
/// Validates <see cref="CorsOptions"/> using a rule that depends on the hosting environment:
/// an empty allowed-origins list is acceptable in Development (where the CORS policy falls back
/// to <c>localhost</c>), but at least one non-empty origin is mandatory in every other environment
/// to avoid silently opening CORS to no origin (or an unconfigured one) in production.
/// </summary>
/// <param name="environment">The hosting environment used to decide whether the rule applies.</param>
public sealed class CorsOptionsValidator(IHostEnvironment environment) : IValidateOptions<CorsOptions>
{
    /// <summary>
    /// Validates the bound <see cref="CorsOptions"/> instance.
    /// </summary>
    /// <param name="name">The name of the options instance being validated.</param>
    /// <param name="options">The bound options instance.</param>
    /// <returns>
    /// A successful result in Development, or when at least one non-empty origin is configured;
    /// otherwise a failure result describing the missing configuration.
    /// </returns>
    public ValidateOptionsResult Validate(string? name, CorsOptions options)
    {
        if (environment.IsDevelopment() || options.AllowedOrigins.Any(o => !string.IsNullOrWhiteSpace(o)))
        {
            return ValidateOptionsResult.Success;
        }

        return ValidateOptionsResult.Fail(
            "Cors:AllowedOrigins must contain at least one origin outside the Development environment.");
    }
}
