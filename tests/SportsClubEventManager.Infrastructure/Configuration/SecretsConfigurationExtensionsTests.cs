using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.KeyPerFile;
using Microsoft.Extensions.FileProviders;
using SportsClubEventManager.Infrastructure.Configuration;
using Xunit;

namespace SportsClubEventManager.Tests.Infrastructure.Configuration;

/// <summary>
/// Unit tests for <see cref="SecretsConfigurationExtensions"/>.
/// </summary>
/// <remarks>
/// <see cref="SecretsConfigurationExtensions.AddDockerSecrets"/> hardcodes its target directory
/// ("/run/secrets") as a private constant with no overload to inject an alternate path. That
/// means the "directory exists and its files are translated into configuration keys" branch
/// cannot be safely exercised end-to-end through the public method itself in a portable,
/// permission-safe way: on Linux CI runners (this repository's "build-and-test" job runs on
/// "ubuntu-latest") "/run" is a root-owned tmpfs that an unprivileged test process cannot create
/// subdirectories under, and on Windows ".NET" resolves the same literal path relative to the
/// current drive (e.g. "C:\run\secrets"), which is equally inappropriate for a test to create/
/// delete on a real machine. Tests below therefore split the verification in two:
/// (1) tests that call the real <see cref="SecretsConfigurationExtensions.AddDockerSecrets"/>
///     method and assert on its observable configuration (the safe no-op path, which is always
///     exercised because the hardcoded directory never exists in dev/CI environments), and
/// (2) a test that reproduces, against a real temporary directory, the exact same
///     <c>KeyPerFileConfigurationSource</c> option recipe ("__" delimiter, Optional = true) that
///     <see cref="SecretsConfigurationExtensions.AddDockerSecrets"/> configures internally, to
///     prove the delimiter-translation behavior the design requires actually works. This second
///     group does not go through the hardcoded path and is called out explicitly below and in the
///     testing summary as a known coverage gap of the "directory exists" branch.
/// </remarks>
public sealed class SecretsConfigurationExtensionsTests
{
    /// <summary>
    /// Verifies that AddDockerSecrets returns the same builder instance so callers can keep
    /// chaining configuration calls (e.g. "builder.Configuration.AddDockerSecrets()").
    /// </summary>
    [Fact]
    public void AddDockerSecrets_ReturnsSameBuilderInstance_ForFluentChaining()
    {
        // Arrange
        var builder = new ConfigurationBuilder();

        // Act
        var result = builder.AddDockerSecrets();

        // Assert
        result.Should().BeSameAs(builder);
    }

    /// <summary>
    /// Verifies that AddDockerSecrets registers exactly one KeyPerFile configuration source,
    /// so calling it does not accidentally add duplicate or unrelated sources.
    /// </summary>
    [Fact]
    public void AddDockerSecrets_AddsExactlyOneKeyPerFileConfigurationSource()
    {
        // Arrange
        var builder = new ConfigurationBuilder();

        // Act
        builder.AddDockerSecrets();

        // Assert
        builder.Sources.OfType<KeyPerFileConfigurationSource>().Should().ContainSingle();
    }

    /// <summary>
    /// Verifies that AddDockerSecrets marks the underlying source as Optional, which is the hard
    /// functional requirement that makes the source a safe no-op when Docker has not mounted
    /// "/run/secrets" (e.g. local "dotnet run" without Docker).
    /// </summary>
    [Fact]
    public void AddDockerSecrets_ConfiguresSourceAsOptional()
    {
        // Arrange
        var builder = new ConfigurationBuilder();

        // Act
        builder.AddDockerSecrets();

        // Assert
        var source = builder.Sources.OfType<KeyPerFileConfigurationSource>().Single();
        source.Optional.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that AddDockerSecrets configures the "__" section delimiter, matching the
    /// convention already used by environment variables in this repository
    /// (e.g. "Authentication__JwtSettings__SecretKey").
    /// </summary>
    [Fact]
    public void AddDockerSecrets_ConfiguresSourceWithDoubleUnderscoreSectionDelimiter()
    {
        // Arrange
        var builder = new ConfigurationBuilder();

        // Act
        builder.AddDockerSecrets();

        // Assert
        var source = builder.Sources.OfType<KeyPerFileConfigurationSource>().Single();
        source.SectionDelimiter.Should().Be("__");
    }

    /// <summary>
    /// Verifies that AddDockerSecrets does not assign a FileProvider when the target directory
    /// does not exist, which is exactly the guard this method uses to avoid the
    /// DirectoryNotFoundException that PhysicalFileProvider's constructor would otherwise throw.
    /// This exercises the real production method's "directory missing" branch (the branch that
    /// always applies in this test environment, since "/run/secrets" is never mounted outside a
    /// running Docker/Kubernetes container).
    /// </summary>
    [Fact]
    public void AddDockerSecrets_WhenSecretsDirectoryDoesNotExist_LeavesFileProviderUnset()
    {
        // Arrange
        var builder = new ConfigurationBuilder();

        // Act
        builder.AddDockerSecrets();

        // Assert
        var source = builder.Sources.OfType<KeyPerFileConfigurationSource>().Single();
        source.FileProvider.Should().BeNull();
    }

    /// <summary>
    /// Verifies the hard functional requirement from the design ("Risks &amp; Open Decisions" /
    /// safe no-op note): calling AddDockerSecrets and building the configuration must never throw
    /// when "/run/secrets" does not exist, simulating local "dotnet run" without Docker.
    /// </summary>
    [Fact]
    public void AddDockerSecrets_WhenSecretsDirectoryDoesNotExist_BuildDoesNotThrow()
    {
        // Arrange
        var builder = new ConfigurationBuilder();

        // Act
        var act = () =>
        {
            builder.AddDockerSecrets();
            builder.Build();
        };

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that, as a consequence of the safe no-op behavior, building the configuration
    /// after AddDockerSecrets produces a valid, queryable IConfigurationRoot that simply has no
    /// value for a key that would only exist if a Docker secret file had been mounted.
    /// </summary>
    [Fact]
    public void AddDockerSecrets_WhenSecretsDirectoryDoesNotExist_BuiltConfigurationHasNoDockerSecretValues()
    {
        // Arrange
        var builder = new ConfigurationBuilder();
        builder.AddDockerSecrets();

        // Act
        var configuration = builder.Build();

        // Assert
        configuration.Should().NotBeNull();
        configuration["Authentication:JwtSettings:SecretKey"].Should().BeNull();
    }

    /// <summary>
    /// Verifies the delimiter-translation contract AddDockerSecrets relies on: a file named with
    /// the repository's "__" convention (here "nombreseccion__subclave") is translated into the
    /// nested configuration key "NombreSeccion:SubClave" once built into an IConfigurationRoot.
    /// Uses a real temporary directory and the identical KeyPerFile option recipe AddDockerSecrets
    /// configures (Optional = true, SectionDelimiter = "__") because the extension method itself
    /// cannot be redirected away from its hardcoded "/run/secrets" path (see class remarks).
    /// </summary>
    [Fact]
    public void KeyPerFileSourceWithDockerSecretsRecipe_WithDoubleUnderscoreDelimitedFile_TranslatesToNestedConfigurationKey()
    {
        // Arrange
        var tempDirectory = Directory.CreateTempSubdirectory("docker-secrets-test-");

        try
        {
            File.WriteAllText(Path.Combine(tempDirectory.FullName, "nombreseccion__subclave"), "valor-secreto");

            var builder = new ConfigurationBuilder();
            builder.AddKeyPerFile(options =>
            {
                options.FileProvider = new PhysicalFileProvider(tempDirectory.FullName);
                options.Optional = true;
                options.SectionDelimiter = "__";
            });

            // Act
            var configuration = builder.Build();

            // Assert
            configuration["NombreSeccion:SubClave"].Should().Be("valor-secreto");
        }
        finally
        {
            tempDirectory.Delete(recursive: true);
        }
    }

    /// <summary>
    /// Verifies the same delimiter-translation contract for a three-segment key, matching real
    /// secrets in this repository's inventory (e.g. "Authentication__JwtSettings__SecretKey"),
    /// not just the simpler two-segment case.
    /// </summary>
    [Fact]
    public void KeyPerFileSourceWithDockerSecretsRecipe_WithThreeSegmentDelimitedFile_TranslatesToNestedConfigurationKey()
    {
        // Arrange
        var tempDirectory = Directory.CreateTempSubdirectory("docker-secrets-test-");

        try
        {
            File.WriteAllText(
                Path.Combine(tempDirectory.FullName, "authentication__jwtsettings__secretkey"),
                "super-secret-jwt-key");

            var builder = new ConfigurationBuilder();
            builder.AddKeyPerFile(options =>
            {
                options.FileProvider = new PhysicalFileProvider(tempDirectory.FullName);
                options.Optional = true;
                options.SectionDelimiter = "__";
            });

            // Act
            var configuration = builder.Build();

            // Assert
            configuration["Authentication:JwtSettings:SecretKey"].Should().Be("super-secret-jwt-key");
        }
        finally
        {
            tempDirectory.Delete(recursive: true);
        }
    }

    /// <summary>
    /// Verifies that a missing, guaranteed-nonexistent directory (a fresh GUID under the temp
    /// root) behaves as a safe no-op with the same option recipe AddDockerSecrets uses, giving a
    /// deterministic version of the no-op requirement that does not depend on "/run/secrets"
    /// happening to be absent on the machine running the test.
    /// </summary>
    [Fact]
    public void KeyPerFileSourceWithDockerSecretsRecipe_WhenDirectoryIsGuaranteedNotToExist_BuildDoesNotThrow()
    {
        // Arrange
        var nonExistentDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.Exists(nonExistentDirectory).Should().BeFalse();

        var builder = new ConfigurationBuilder();

        // Act
        var act = () =>
        {
            builder.AddKeyPerFile(options =>
            {
                if (Directory.Exists(nonExistentDirectory))
                {
                    options.FileProvider = new PhysicalFileProvider(nonExistentDirectory);
                }

                options.Optional = true;
                options.SectionDelimiter = "__";
            });

            builder.Build();
        };

        // Assert
        act.Should().NotThrow();
    }
}
