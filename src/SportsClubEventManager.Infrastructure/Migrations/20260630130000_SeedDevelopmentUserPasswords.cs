// DEV ONLY — Do not apply to production.
// Apply manually: dotnet ef database update SeedDevelopmentUserPasswords
// All seed users share the password: Password1!

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportsClubEventManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedDevelopmentUserPasswords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            if (!string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // BCrypt work factor 12 — all users share the password: Password1!
            migrationBuilder.Sql(@"
                UPDATE Users SET
                    PasswordHash = '$2a$12$H5fPLh116f.2A243ywsrFOXQU8roTvoFEv9miyfAA/JeQAdyKH2Yq',
                    ProviderName = 'Local'
                WHERE Id = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';

                UPDATE Users SET
                    PasswordHash = '$2a$12$XMlnCpM2VxHvR43T1HJYw.Pb11SSkF47DrQRqFDrrcTCx90/Ho47u',
                    ProviderName = 'Local'
                WHERE Id = 'cccccccc-cccc-cccc-cccc-cccccccccccc';

                UPDATE Users SET
                    PasswordHash = '$2a$12$BTEHKXGsBkfgwFlAXS1cLen.uGgmoqw3OGUqmp/FCcovP6wWxHdoa',
                    ProviderName = 'Local'
                WHERE Id = 'dddddddd-dddd-dddd-dddd-dddddddddddd';

                UPDATE Users SET
                    PasswordHash = '$2a$12$VBJY8/m1WRO5.nQWCigE7uQcCO7k8.OMDotrwKR3cVfxQ3cLMUeQa',
                    ProviderName = 'Local'
                WHERE Id = 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee';

                UPDATE Users SET
                    PasswordHash = '$2a$12$DEB2/UvKY8xwWC1.bDaHn./uyb9EUMsQxnolSFIcdld1YUDNf9/.e',
                    ProviderName = 'Local'
                WHERE Id = 'ffffffff-ffff-ffff-ffff-ffffffffffff';

                UPDATE Users SET
                    PasswordHash = '$2a$12$l332X/KWkPzwq5JiwuyAZuXdvDep0mqMooEBbVDrvo2jBhgja/ijG',
                    ProviderName = 'Local'
                WHERE Id = '10101010-1010-1010-1010-101010101010';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            if (!string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            migrationBuilder.Sql(@"
                UPDATE Users SET PasswordHash = NULL, ProviderName = NULL
                WHERE Id IN (
                    'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
                    'cccccccc-cccc-cccc-cccc-cccccccccccc',
                    'dddddddd-dddd-dddd-dddd-dddddddddddd',
                    'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee',
                    'ffffffff-ffff-ffff-ffff-ffffffffffff',
                    '10101010-1010-1010-1010-101010101010'
                );
            ");
        }
    }
}
