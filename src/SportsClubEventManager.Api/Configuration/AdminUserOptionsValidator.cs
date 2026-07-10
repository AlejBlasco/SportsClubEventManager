using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace SportsClubEventManager.Api.Configuration;

/// <summary>
/// Validates <see cref="AdminUserOptions"/> using a rule that depends on the hosting
/// environment: the seeded administrator password is optional in Development (where a
/// well-known local seed password is acceptable), but mandatory in every other environment.
/// </summary>
/// <param name="environment">The hosting environment used to decide whether the rule applies.</param>
public sealed class AdminUserOptionsValidator(IHostEnvironment environment) : IValidateOptions<AdminUserOptions>
{
    /// <summary>
    /// Validates the bound <see cref="AdminUserOptions"/> instance.
    /// </summary>
    /// <param name="name">The name of the options instance being validated.</param>
    /// <param name="options">The bound options instance.</param>
    /// <returns>
    /// A successful result in Development, or when a non-empty password is configured;
    /// otherwise a failure result describing how to set the missing value.
    /// </returns>
    public ValidateOptionsResult Validate(string? name, AdminUserOptions options)
    {
        if (environment.IsDevelopment() || !string.IsNullOrWhiteSpace(options.Password))
        {
            return ValidateOptionsResult.Success;
        }

        return ValidateOptionsResult.Fail(
            "AdminUser:Password is required outside the Development environment. " +
            "Set it via User Secrets (local) or the ADMIN_PASSWORD environment variable (Docker).");
    }
}
