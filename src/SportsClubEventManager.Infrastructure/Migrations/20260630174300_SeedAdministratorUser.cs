using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;

#nullable disable

namespace SportsClubEventManager.Infrastructure.Migrations
{
    /// <summary>
    /// Migration to seed the initial Administrator user account.
    /// Password is read from configuration (User Secrets in development, Azure Key Vault in production).
    /// </summary>
    public partial class SeedAdministratorUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Build configuration to read admin password from User Secrets / Azure Key Vault
            var configuration = new ConfigurationBuilder()
                .AddUserSecrets<SeedAdministratorUser>(optional: true)
                .AddEnvironmentVariables()
                .Build();

            var adminPassword = configuration["AdminUser:Password"];

            if (string.IsNullOrWhiteSpace(adminPassword))
            {
                throw new InvalidOperationException(
                    "Administrator password is not configured. " +
                    "Set the 'AdminUser:Password' configuration value in User Secrets (development) or Azure Key Vault (production). " +
                    "Example for User Secrets: dotnet user-secrets set \"AdminUser:Password\" \"YourSecurePassword123!\" --project src/SportsClubEventManager.Infrastructure");
            }

            // Hash the password using BCrypt with work factor 12
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword, workFactor: 12);

            var adminId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var now = DateTime.UtcNow;

            migrationBuilder.Sql($@"
                IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'admin@sportsclub.local')
                BEGIN
                    INSERT INTO Users (Id, Name, Gender, Email, LicenseNumber, LicenseCategory, CreatedAt, UpdatedAt, PasswordHash, ExternalProviderId, ProviderName, RefreshToken, RefreshTokenExpiryTime, IsActive, LastLoginAt, Role)
                    VALUES (
                        '{adminId}',
                        'System Administrator',
                        'Other',
                        'admin@sportsclub.local',
                        NULL,
                        NULL,
                        '{now:yyyy-MM-dd HH:mm:ss.fff}',
                        NULL,
                        '{passwordHash}',
                        NULL,
                        'Local',
                        NULL,
                        NULL,
                        1,
                        NULL,
                        'Administrator'
                    );
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the seeded administrator user
            migrationBuilder.Sql(@"
                DELETE FROM Users WHERE Email = 'admin@sportsclub.local' AND ProviderName = 'Local';
            ");
        }
    }
}
