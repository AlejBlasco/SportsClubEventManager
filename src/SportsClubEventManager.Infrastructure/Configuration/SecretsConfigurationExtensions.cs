using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace SportsClubEventManager.Infrastructure.Configuration;

/// <summary>
/// Extension methods that add file-based secret sources to <see cref="IConfigurationBuilder"/>.
/// </summary>
public static class SecretsConfigurationExtensions
{
    /// <summary>
    /// Default mount path used by Docker/Kubernetes for file-based secrets.
    /// </summary>
    private const string DefaultSecretsDirectory = "/run/secrets";

    /// <summary>
    /// Adds a configuration source that reads secrets mounted as individual files
    /// (Docker Compose "secrets:", Docker Swarm secrets, or Kubernetes Secret volumes)
    /// from <see cref="DefaultSecretsDirectory"/>. File names use the same "__" delimiter
    /// convention already used by environment variables in this repository
    /// (e.g. a file named "authentication__jwtsettings__secretkey" maps to the
    /// configuration key "Authentication:JwtSettings:SecretKey").
    /// This source is appended LAST, after the framework's default chain
    /// (appsettings.json, appsettings.{Environment}.json, User Secrets, environment
    /// variables), giving it the highest precedence. It is a safe no-op when the
    /// directory does not exist (local "dotnet run" without Docker).
    /// </summary>
    public static IConfigurationBuilder AddDockerSecrets(this IConfigurationBuilder builder)
    {
        return builder.AddKeyPerFile(options =>
        {
            // KeyPerFileConfigurationSource (10.0.9) has no "SecretsDirectoryPath" property;
            // the directory is instead supplied via a FileProvider. Only construct the
            // PhysicalFileProvider when the directory actually exists: its constructor throws
            // DirectoryNotFoundException otherwise, which would defeat the safe no-op behavior
            // this method must guarantee for "dotnet run" without Docker.
            if (Directory.Exists(DefaultSecretsDirectory))
            {
                options.FileProvider = new PhysicalFileProvider(DefaultSecretsDirectory);
            }

            options.Optional = true;

            // Named "SectionDelimiter" (not "KeyDelimiter") in this package version; "__" is
            // already its default, set explicitly here to document the intent regardless of
            // upstream default changes.
            options.SectionDelimiter = "__";
        });
    }
}
