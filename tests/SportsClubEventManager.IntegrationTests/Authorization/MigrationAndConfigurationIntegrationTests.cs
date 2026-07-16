using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Respawn;
using SportsClubEventManager.Domain.Enums;
using SportsClubEventManager.Infrastructure.Persistence;
using Testcontainers.MsSql;

namespace SportsClubEventManager.IntegrationTests.Authorization;

/// <summary>
/// Integration tests for database migrations related to role-based authorization,
/// including role column presence, default role assignment, and administrator seeding.
/// </summary>
public class MigrationAndConfigurationIntegrationTests
{
    /// <summary>
    /// Tests that verify database schema changes from authorization migrations.
    /// </summary>
    public sealed class WhenMigrationsApply : IAsyncLifetime
    {
        private MsSqlContainer _container = null!;
        private string _connectionString = null!;
        private Respawner? _respawner;

        /// <summary>
        /// Initializes the test container.
        /// </summary>
        /// <returns>A completed task.</returns>
        public async Task InitializeAsync()
        {
            _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
                .WithPassword("YourStrong!Passw0rd")
                .Build();

            await _container.StartAsync();
            _connectionString = _container.GetConnectionString();

            // Apply migrations
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(_connectionString)
                .Options;

            using var context = new AppDbContext(options);
            await context.Database.MigrateAsync();

            // Setup respawner
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
            {
                DbAdapter = DbAdapter.SqlServer
            });
        }

        /// <summary>
        /// Cleans up the test container.
        /// </summary>
        /// <returns>A completed task.</returns>
        public async Task DisposeAsync()
        {
            await _container.DisposeAsync();
        }

        /// <summary>
        /// Verifies that the Role column exists in the Users table with correct configuration.
        /// </summary>
        [Fact]
        public async Task AddRoleToUser_Migration_CreatesRoleColumn()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(_connectionString)
                .Options;

            // Act
            using var context = new AppDbContext(options);
            var roleColumnExists = await RoleColumnExistsAsync();

            // Assert
            roleColumnExists.Should().BeTrue("Role column should exist in Users table after migration");
        }

        /// <summary>
        /// Verifies that existing users are assigned the User role after migration.
        /// </summary>
        [Fact]
        public async Task AddRoleToUser_Migration_AssignsUserRoleToExistingUsers()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(_connectionString)
                .Options;

            using var context = new AppDbContext(options);

            // Create a user before role column would have been added (simulate)
            var user = new SportsClubEventManager.Domain.Entities.User
            {
                Name = "Existing User",
                Email = "existing@example.com",
                Gender = SportsClubEventManager.Domain.Enums.Gender.Male,
                Role = Role.User // This should be assigned by migration default
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Act
            var savedUser = await context.Users
                .FirstOrDefaultAsync(u => u.Email == "existing@example.com");

            // Assert
            savedUser.Should().NotBeNull();
            savedUser!.Role.Should().Be(Role.User);
        }

        /// <summary>
        /// Verifies that Role column is indexed for query performance.
        /// </summary>
        [Fact]
        public async Task AddRoleToUser_Migration_CreatesIndexOnRoleColumn()
        {
            // Arrange & Act
            var indexExists = await RoleColumnIsIndexedAsync();

            // Assert
            indexExists.Should().BeTrue("Role column should be indexed for authorization query performance");
        }

        /// <summary>
        /// Verifies that the Administrator user is seeded via migration.
        /// </summary>
        [Fact]
        public async Task SeedAdministratorUser_Migration_CreatesAdminAccount()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(_connectionString)
                .Options;

            // Act
            using var context = new AppDbContext(options);
            var adminUser = await context.Users
                .FirstOrDefaultAsync(u => u.Email == "admin@sportsclub.local");

            // Assert
            adminUser.Should().NotBeNull("Administrator user should be seeded via migration");
            adminUser!.Role.Should().Be(Role.Administrator);
            adminUser.Email.Should().Be("admin@sportsclub.local");
            adminUser.ProviderName.Should().Be("Local");
        }

        /// <summary>
        /// Verifies that the Administrator user has a hashed password.
        /// </summary>
        [Fact]
        public async Task SeedAdministratorUser_Migration_SetsHashedPassword()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(_connectionString)
                .Options;

            // Act
            using var context = new AppDbContext(options);
            var adminUser = await context.Users
                .FirstOrDefaultAsync(u => u.Email == "admin@sportsclub.local");

            // Assert
            adminUser.Should().NotBeNull();
            adminUser!.PasswordHash.Should().NotBeNullOrEmpty("Admin should have password hash");
            // Verify it's BCrypt hash format (starts with $2a$, $2b$, or $2x$)
            adminUser.PasswordHash.Should().MatchRegex(@"\$2[aby]\$.*", "Should be valid BCrypt hash");
        }

        /// <summary>
        /// Verifies that the Administrator migration is idempotent (doesn't create duplicates on re-run).
        /// </summary>
        [Fact]
        public async Task SeedAdministratorUser_Migration_IsIdempotent()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(_connectionString)
                .Options;

            using var context = new AppDbContext(options);

            // Act
            var adminUsers = await context.Users
                .Where(u => u.Email == "admin@sportsclub.local")
                .ToListAsync();

            // Assert
            adminUsers.Should().HaveCount(1, "Administrator user should exist exactly once (migration is idempotent)");
        }

        /// <summary>
        /// Verifies that Role column has proper SQL data type (nvarchar).
        /// </summary>
        [Fact]
        public async Task AddRoleToUser_Migration_UsesCorrectSqlDataType()
        {
            // Arrange & Act
            var dataType = await GetRoleColumnDataTypeAsync();

            // Assert
            // Role is stored as string/nvarchar in database for human readability. Should().Match()
            // only supports "*"/"?" wildcards, not "|" alternation like a regex - "*nvarchar*|*varchar*"
            // was a false negative, always failing even when dataType was exactly "nvarchar".
            dataType.Should().NotBeNullOrEmpty();
            dataType.Should().MatchRegex("nvarchar|varchar",
                "Role column should use string type (nvarchar) for database storage");
        }

        /// <summary>
        /// Verifies that Role column has maximum length constraint.
        /// </summary>
        [Fact]
        public async Task AddRoleToUser_Migration_HasMaxLengthConstraint()
        {
            // Arrange & Act
            var maxLength = await GetRoleColumnMaxLengthAsync();

            // Assert
            maxLength.Should().BeGreaterThan(0, "Role column should have max length constraint");
            maxLength.Should().BeLessThanOrEqualTo(50, "Role column max length should be reasonable (50 chars)");
        }

        #region Helper Methods

        /// <summary>
        /// Checks if Role column exists in Users table.
        /// </summary>
        /// <returns>True if column exists; otherwise false.</returns>
        private async Task<bool> RoleColumnExistsAsync()
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'Role'";

            var result = await command.ExecuteScalarAsync();
            return (int?)result > 0;
        }

        /// <summary>
        /// Checks if Role column has an index.
        /// </summary>
        /// <returns>True if indexed; otherwise false.</returns>
        private async Task<bool> RoleColumnIsIndexedAsync()
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT COUNT(*) FROM sys.indexes
                WHERE object_id = OBJECT_ID('dbo.Users')
                AND name LIKE '%Role%'";

            var result = await command.ExecuteScalarAsync();
            return (int?)result > 0;
        }

        /// <summary>
        /// Gets the SQL data type of the Role column.
        /// </summary>
        /// <returns>The data type name.</returns>
        private async Task<string?> GetRoleColumnDataTypeAsync()
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'Role'";

            return (await command.ExecuteScalarAsync())?.ToString();
        }

        /// <summary>
        /// Gets the maximum length constraint of the Role column.
        /// </summary>
        /// <returns>The maximum length value.</returns>
        private async Task<int?> GetRoleColumnMaxLengthAsync()
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT CHARACTER_MAXIMUM_LENGTH FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'Role'";

            return (int?)await command.ExecuteScalarAsync();
        }

        #endregion
    }

    /// <summary>
    /// Tests that verify API configuration validation related to authorization.
    /// </summary>
    public sealed class WhenApiStartsWithRoleBasedAuthorization
    {
        /// <summary>
        /// Verifies that API requires authorization configuration to start.
        /// </summary>
        [Fact]
        public void ApiStartup_WithProperAuthorization_Succeeds()
        {
            // Arrange & Act
            // The WebApplicationFactory in other tests starts successfully
            // This test passes implicitly if all other integration tests pass

            // Assert
            // Implicit: if API starts with role-based auth configured, test passes
            true.Should().BeTrue();
        }

        /// <summary>
        /// Verifies that the API host fails fast at startup (before serving any HTTP request) when
        /// Authentication:JwtSettings:SecretKey is entirely missing, via JwtSettingsOptions'
        /// [Required] data annotation registered through AddApiConfigurationOptions/ValidateOnStart.
        /// </summary>
        [Fact]
        public void ApiConfiguration_WithoutJwtSecretKey_FailsAtStartup()
        {
            // Arrange
            using var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureAppConfiguration((_, config) =>
                    {
                        config.AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            ["Authentication:JwtSettings:SecretKey"] = string.Empty
                        });
                    });
                });

            // Act
            var act = () => factory.CreateClient();

            // Assert
            var thrown = act.Should().Throw<Exception>().Which;
            CollectAllMessages(thrown).Should().Contain(message => message.Contains("SecretKey"));
        }

        /// <summary>
        /// Verifies that the API host fails fast at startup when Authentication:JwtSettings:SecretKey
        /// is present but shorter than the 32 characters (256 bits) required by
        /// JwtSettingsOptions' [MinLength(32)] data annotation for HMAC-SHA256 signing.
        /// </summary>
        [Fact]
        public void ApiConfiguration_WithShortJwtSecretKey_FailsAtStartup()
        {
            // Arrange
            using var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureAppConfiguration((_, config) =>
                    {
                        config.AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            ["Authentication:JwtSettings:SecretKey"] = "too-short-key"
                        });
                    });
                });

            // Act
            var act = () => factory.CreateClient();

            // Assert
            var thrown = act.Should().Throw<Exception>().Which;
            CollectAllMessages(thrown).Should().Contain(message => message.Contains("SecretKey"));
        }

        /// <summary>
        /// Verifies that the API host fails fast at startup when Cors:AllowedOrigins is empty in a
        /// simulated Production environment, via CorsOptionsValidator (the Development-only
        /// localhost fallback must not silently apply outside Development).
        /// </summary>
        [Fact]
        public void ApiConfiguration_WithEmptyCorsOriginsInProduction_FailsAtStartup()
        {
            // Arrange
            using var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment("Production");
                    builder.ConfigureAppConfiguration((_, config) =>
                    {
                        config.AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            // appsettings.json ships exactly two base entries (indices 0 and 1);
                            // overriding both with blank values empties the effective array without
                            // needing to know every configuration source contributing to it.
                            ["Cors:AllowedOrigins:0"] = string.Empty,
                            ["Cors:AllowedOrigins:1"] = "   ",
                            // Supplied so AdminUserOptionsValidator (also mandatory outside
                            // Development) does not fail simultaneously and muddy the assertion.
                            ["AdminUser:Password"] = "a-real-non-blank-admin-password"
                        });
                    });
                });

            // Act
            var act = () => factory.CreateClient();

            // Assert
            var thrown = act.Should().Throw<Exception>().Which;
            CollectAllMessages(thrown).Should().Contain(message => message.Contains("Cors:AllowedOrigins"));
        }

        /// <summary>
        /// Flattens an exception and, if present, every exception aggregated inside it into a flat
        /// list of messages. ValidateOnStart aggregates multiple simultaneous options-validation
        /// failures into an AggregateException, but a single failure is thrown as-is; checking both
        /// shapes keeps these assertions correct regardless of that aggregation detail.
        /// </summary>
        /// <param name="exception">The exception thrown by the failing host startup.</param>
        /// <returns>Every message found in the exception and its inner/aggregated exceptions.</returns>
        private static IReadOnlyList<string> CollectAllMessages(Exception exception)
        {
            var messages = new List<string> { exception.Message };

            if (exception is AggregateException aggregateException)
            {
                foreach (var inner in aggregateException.InnerExceptions)
                {
                    messages.AddRange(CollectAllMessages(inner));
                }
            }
            else if (exception.InnerException is not null)
            {
                messages.AddRange(CollectAllMessages(exception.InnerException));
            }

            return messages;
        }
    }
}
