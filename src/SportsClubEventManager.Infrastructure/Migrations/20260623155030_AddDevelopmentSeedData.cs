// DEV ONLY — Do not apply to production.
// Apply manually: dotnet ef database update AddDevelopmentSeedData

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SportsClubEventManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDevelopmentSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            if (string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase))
            {
                migrationBuilder.InsertData(
                    table: "Events",
                    columns: new[] { "Id", "CreatedAt", "Date", "Description", "Location", "MaxCapacity", "Title", "UpdatedAt" },
                    values: new object[,]
                    {
                        { new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2025, 12, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 15, 10, 0, 0, 0, DateTimeKind.Utc), "Social shooting competition for 9mm pistol enthusiasts. Open to all categories.", "Zaragoza - CTZ", 20, "Pistola 9mm - Social", null },
                        { new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 3, 20, 9, 30, 0, 0, DateTimeKind.Utc), "BR50 rifle trophy competition. Precision benchrest shooting at 50 meters.", "Teruel - CT Aguanaces", 15, "Carabina BR50 - Trofeo", null },
                        { new Guid("33333333-3333-3333-3333-333333333333"), new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 28, 10, 0, 0, 0, DateTimeKind.Utc), "Regional championship for air pistol. Part of the Aragon Cup series.", "Huesca - RTAA", 15, "Aire Comprimido Pistola - Copa Aragón", null },
                        { new Guid("44444444-4444-4444-4444-444444444444"), new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 7, 5, 9, 0, 0, 0, DateTimeKind.Utc), "Provincial championship for standard pistol category. Includes precision and rapid fire stages.", "Madrid - RFEDETO", 12, "Pistola Estándar - Campeonato Provincial", null },
                        { new Guid("55555555-5555-5555-5555-555555555555"), new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 7, 12, 10, 30, 0, 0, DateTimeKind.Utc), "Friendly air rifle competition. Perfect for beginners and experienced shooters alike.", "Barcelona - CT Vallès", 7, "Aire Comprimido Carabina - Social", null },
                        { new Guid("66666666-6666-6666-6666-666666666666"), new DateTime(2026, 6, 5, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 7, 18, 11, 0, 0, 0, DateTimeKind.Utc), "Speed shooting competition. Fast-paced action with precision requirements.", "Valencia - CTW", 6, "Pistola de Velocidad - Trofeo del Globo", null },
                        { new Guid("77777777-7777-7777-7777-777777777777"), new DateTime(2026, 6, 10, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 7, 25, 9, 0, 0, 0, DateTimeKind.Utc), "3-position rifle championship. Prone, standing, and kneeling positions at 50m.", "Zaragoza - CTZ", 15, "Carabina 3x20 - Campeonato Provincial", null },
                        { new Guid("88888888-8888-8888-8888-888888888888"), new DateTime(2026, 6, 15, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 7, 30, 8, 30, 0, 0, DateTimeKind.Utc), "50 meter precision pistol competition. Olympic discipline, high precision required.", "Teruel - CT Aguanaces", 40, "Pistola 50m - Copa Aragón", null },
                        { new Guid("99999999-9999-9999-9999-999999999999"), new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 10, 9, 0, 0, 0, DateTimeKind.Utc), "Clay pigeon shooting competition. Trap discipline with variable trajectories.", "Huesca - RTAA", 30, "Trap - Trofeo", null },
                        { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 12, 5, 10, 0, 0, 0, DateTimeKind.Utc), "Universal trench shooting event. End-of-season friendly competition.", "Madrid - RFEDETO", 20, "Foso Universal - Social", null }
                    });

                migrationBuilder.InsertData(
                    table: "Users",
                    columns: new[] { "Id", "CreatedAt", "Email", "Gender", "LicenseCategory", "LicenseNumber", "Name", "UpdatedAt" },
                    values: new object[,]
                    {
                        { new Guid("10101010-1010-1010-1010-101010101010"), new DateTime(2026, 1, 20, 0, 0, 0, 0, DateTimeKind.Utc), "carlos.jimenez@example.com", "Male", "A", "ESP-2026-005", "Carlos Jiménez Moreno", null },
                        { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), new DateTime(2025, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "carmen.garcia@example.com", "Female", "A", "ESP-2026-001", "Carmen García López", null },
                        { new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), new DateTime(2025, 11, 15, 0, 0, 0, 0, DateTimeKind.Utc), "javier.martinez@example.com", "Male", "S", "ESP-2026-002", "Javier Martínez Ruiz", null },
                        { new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"), new DateTime(2025, 12, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ana.fernandez@example.com", "Female", "A", "ESP-2026-003", "Ana Fernández Pérez", null },
                        { new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), new DateTime(2025, 12, 10, 0, 0, 0, 0, DateTimeKind.Utc), "miguel.sanchez@example.com", "Male", null, null, "Miguel Sánchez Torres", null },
                        { new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"), new DateTime(2026, 1, 5, 0, 0, 0, 0, DateTimeKind.Utc), "laura.rodriguez@example.com", "Female", "S", "ESP-2026-004", "Laura Rodríguez Gómez", null }
                    });

                migrationBuilder.InsertData(
                    table: "Registrations",
                    columns: new[] { "Id", "CreatedAt", "EventId", "RegistrationDate", "Status", "UpdatedAt", "UserId" },
                    values: new object[,]
                    {
                        { new Guid("20000001-0000-0000-0000-000000000001"), new DateTime(2026, 2, 5, 10, 0, 0, 0, DateTimeKind.Utc), new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2026, 2, 5, 10, 0, 0, 0, DateTimeKind.Utc), "Registered", null, new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb") },
                        { new Guid("20000002-0000-0000-0000-000000000002"), new DateTime(2026, 2, 10, 14, 30, 0, 0, DateTimeKind.Utc), new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2026, 2, 10, 14, 30, 0, 0, DateTimeKind.Utc), "Registered", null, new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc") },
                        { new Guid("30000001-0000-0000-0000-000000000001"), new DateTime(2026, 5, 10, 9, 0, 0, 0, DateTimeKind.Utc), new Guid("33333333-3333-3333-3333-333333333333"), new DateTime(2026, 5, 10, 9, 0, 0, 0, DateTimeKind.Utc), "Registered", null, new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd") },
                        { new Guid("30000002-0000-0000-0000-000000000002"), new DateTime(2026, 5, 15, 11, 20, 0, 0, DateTimeKind.Utc), new Guid("33333333-3333-3333-3333-333333333333"), new DateTime(2026, 5, 15, 11, 20, 0, 0, DateTimeKind.Utc), "Registered", null, new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee") },
                        { new Guid("30000003-0000-0000-0000-000000000003"), new DateTime(2026, 5, 20, 16, 45, 0, 0, DateTimeKind.Utc), new Guid("33333333-3333-3333-3333-333333333333"), new DateTime(2026, 5, 20, 16, 45, 0, 0, DateTimeKind.Utc), "Registered", null, new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff") },
                        { new Guid("40000001-0000-0000-0000-000000000001"), new DateTime(2026, 5, 16, 8, 0, 0, 0, DateTimeKind.Utc), new Guid("44444444-4444-4444-4444-444444444444"), new DateTime(2026, 5, 16, 8, 0, 0, 0, DateTimeKind.Utc), "Registered", null, new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb") },
                        { new Guid("40000002-0000-0000-0000-000000000002"), new DateTime(2026, 5, 16, 9, 30, 0, 0, DateTimeKind.Utc), new Guid("44444444-4444-4444-4444-444444444444"), new DateTime(2026, 5, 16, 9, 30, 0, 0, DateTimeKind.Utc), "Registered", null, new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc") },
                        { new Guid("40000003-0000-0000-0000-000000000003"), new DateTime(2026, 5, 17, 10, 15, 0, 0, DateTimeKind.Utc), new Guid("44444444-4444-4444-4444-444444444444"), new DateTime(2026, 5, 17, 10, 15, 0, 0, DateTimeKind.Utc), "Registered", null, new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd") },
                        { new Guid("40000004-0000-0000-0000-000000000004"), new DateTime(2026, 5, 18, 11, 0, 0, 0, DateTimeKind.Utc), new Guid("44444444-4444-4444-4444-444444444444"), new DateTime(2026, 5, 18, 11, 0, 0, 0, DateTimeKind.Utc), "Registered", null, new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee") },
                        { new Guid("40000005-0000-0000-0000-000000000005"), new DateTime(2026, 5, 19, 12, 30, 0, 0, DateTimeKind.Utc), new Guid("44444444-4444-4444-4444-444444444444"), new DateTime(2026, 5, 19, 12, 30, 0, 0, DateTimeKind.Utc), "Registered", null, new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff") },
                        { new Guid("40000006-0000-0000-0000-000000000006"), new DateTime(2026, 5, 20, 13, 15, 0, 0, DateTimeKind.Utc), new Guid("44444444-4444-4444-4444-444444444444"), new DateTime(2026, 5, 20, 13, 15, 0, 0, DateTimeKind.Utc), "Registered", null, new Guid("10101010-1010-1010-1010-101010101010") },
                        { new Guid("40000007-0000-0000-0000-000000000007"), new DateTime(2026, 5, 21, 14, 0, 0, 0, DateTimeKind.Utc), new Guid("44444444-4444-4444-4444-444444444444"), new DateTime(2026, 5, 21, 14, 0, 0, 0, DateTimeKind.Utc), "Cancelled", new DateTime(2026, 5, 22, 10, 0, 0, 0, DateTimeKind.Utc), new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb") },
                        { new Guid("50000001-0000-0000-0000-000000000001"), new DateTime(2026, 6, 2, 8, 0, 0, 0, DateTimeKind.Utc), new Guid("55555555-5555-5555-5555-555555555555"), new DateTime(2026, 6, 2, 8, 0, 0, 0, DateTimeKind.Utc), "Registered", null, new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb") },
                        { new Guid("50000002-0000-0000-0000-000000000002"), new DateTime(2026, 6, 2, 8, 15, 0, 0, DateTimeKind.Utc), new Guid("55555555-5555-5555-5555-555555555555"), new DateTime(2026, 6, 2, 8, 15, 0, 0, DateTimeKind.Utc), "Registered", null, new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc") },
                        { new Guid("50000003-0000-0000-0000-000000000003"), new DateTime(2026, 6, 2, 8, 30, 0, 0, DateTimeKind.Utc), new Guid("55555555-5555-5555-5555-555555555555"), new DateTime(2026, 6, 2, 8, 30, 0, 0, DateTimeKind.Utc), "Registered", null, new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd") },
                        { new Guid("50000004-0000-0000-0000-000000000004"), new DateTime(2026, 6, 2, 8, 45, 0, 0, DateTimeKind.Utc), new Guid("55555555-5555-5555-5555-555555555555"), new DateTime(2026, 6, 2, 8, 45, 0, 0, DateTimeKind.Utc), "Registered", null, new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee") },
                        { new Guid("50000005-0000-0000-0000-000000000005"), new DateTime(2026, 6, 2, 9, 0, 0, 0, DateTimeKind.Utc), new Guid("55555555-5555-5555-5555-555555555555"), new DateTime(2026, 6, 2, 9, 0, 0, 0, DateTimeKind.Utc), "Registered", null, new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff") },
                        { new Guid("50000006-0000-0000-0000-000000000006"), new DateTime(2026, 6, 2, 9, 15, 0, 0, DateTimeKind.Utc), new Guid("55555555-5555-5555-5555-555555555555"), new DateTime(2026, 6, 2, 9, 15, 0, 0, DateTimeKind.Utc), "Registered", null, new Guid("10101010-1010-1010-1010-101010101010") },
                        { new Guid("60000001-0000-0000-0000-000000000001"), new DateTime(2026, 6, 6, 8, 0, 0, 0, DateTimeKind.Utc), new Guid("66666666-6666-6666-6666-666666666666"), new DateTime(2026, 6, 6, 8, 0, 0, 0, DateTimeKind.Utc), "Registered", null, new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb") },
                        { new Guid("60000002-0000-0000-0000-000000000002"), new DateTime(2026, 6, 6, 8, 5, 0, 0, DateTimeKind.Utc), new Guid("66666666-6666-6666-6666-666666666666"), new DateTime(2026, 6, 6, 8, 5, 0, 0, DateTimeKind.Utc), "Registered", null, new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc") },
                        { new Guid("60000003-0000-0000-0000-000000000003"), new DateTime(2026, 6, 6, 8, 10, 0, 0, DateTimeKind.Utc), new Guid("66666666-6666-6666-6666-666666666666"), new DateTime(2026, 6, 6, 8, 10, 0, 0, DateTimeKind.Utc), "Registered", null, new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd") },
                        { new Guid("60000004-0000-0000-0000-000000000004"), new DateTime(2026, 6, 6, 8, 15, 0, 0, DateTimeKind.Utc), new Guid("66666666-6666-6666-6666-666666666666"), new DateTime(2026, 6, 6, 8, 15, 0, 0, DateTimeKind.Utc), "Registered", null, new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee") },
                        { new Guid("60000005-0000-0000-0000-000000000005"), new DateTime(2026, 6, 6, 8, 20, 0, 0, DateTimeKind.Utc), new Guid("66666666-6666-6666-6666-666666666666"), new DateTime(2026, 6, 6, 8, 20, 0, 0, DateTimeKind.Utc), "Registered", null, new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff") },
                        { new Guid("60000006-0000-0000-0000-000000000006"), new DateTime(2026, 6, 6, 8, 25, 0, 0, DateTimeKind.Utc), new Guid("66666666-6666-6666-6666-666666666666"), new DateTime(2026, 6, 6, 8, 25, 0, 0, DateTimeKind.Utc), "Registered", null, new Guid("10101010-1010-1010-1010-101010101010") },
                        { new Guid("70000001-0000-0000-0000-000000000001"), new DateTime(2026, 6, 11, 9, 0, 0, 0, DateTimeKind.Utc), new Guid("77777777-7777-7777-7777-777777777777"), new DateTime(2026, 6, 11, 9, 0, 0, 0, DateTimeKind.Utc), "Registered", null, new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb") },
                        { new Guid("70000002-0000-0000-0000-000000000002"), new DateTime(2026, 6, 11, 9, 15, 0, 0, DateTimeKind.Utc), new Guid("77777777-7777-7777-7777-777777777777"), new DateTime(2026, 6, 11, 9, 15, 0, 0, DateTimeKind.Utc), "Registered", null, new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc") },
                        { new Guid("70000003-0000-0000-0000-000000000003"), new DateTime(2026, 6, 11, 9, 30, 0, 0, DateTimeKind.Utc), new Guid("77777777-7777-7777-7777-777777777777"), new DateTime(2026, 6, 11, 9, 30, 0, 0, DateTimeKind.Utc), "Registered", null, new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd") },
                        { new Guid("70000004-0000-0000-0000-000000000004"), new DateTime(2026, 6, 11, 9, 45, 0, 0, DateTimeKind.Utc), new Guid("77777777-7777-7777-7777-777777777777"), new DateTime(2026, 6, 11, 9, 45, 0, 0, DateTimeKind.Utc), "Cancelled", new DateTime(2026, 6, 12, 10, 0, 0, 0, DateTimeKind.Utc), new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee") },
                        { new Guid("70000005-0000-0000-0000-000000000005"), new DateTime(2026, 6, 11, 10, 0, 0, 0, DateTimeKind.Utc), new Guid("77777777-7777-7777-7777-777777777777"), new DateTime(2026, 6, 11, 10, 0, 0, 0, DateTimeKind.Utc), "Registered", null, new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff") },
                        { new Guid("70000006-0000-0000-0000-000000000006"), new DateTime(2026, 6, 11, 10, 15, 0, 0, DateTimeKind.Utc), new Guid("77777777-7777-7777-7777-777777777777"), new DateTime(2026, 6, 11, 10, 15, 0, 0, DateTimeKind.Utc), "Registered", null, new Guid("10101010-1010-1010-1010-101010101010") },
                        { new Guid("80000001-0000-0000-0000-000000000001"), new DateTime(2026, 6, 16, 8, 0, 0, 0, DateTimeKind.Utc), new Guid("88888888-8888-8888-8888-888888888888"), new DateTime(2026, 6, 16, 8, 0, 0, 0, DateTimeKind.Utc), "Registered", null, new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb") },
                        { new Guid("80000002-0000-0000-0000-000000000002"), new DateTime(2026, 6, 16, 8, 30, 0, 0, DateTimeKind.Utc), new Guid("88888888-8888-8888-8888-888888888888"), new DateTime(2026, 6, 16, 8, 30, 0, 0, DateTimeKind.Utc), "Registered", null, new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc") },
                        { new Guid("80000003-0000-0000-0000-000000000003"), new DateTime(2026, 6, 16, 9, 0, 0, 0, DateTimeKind.Utc), new Guid("88888888-8888-8888-8888-888888888888"), new DateTime(2026, 6, 16, 9, 0, 0, 0, DateTimeKind.Utc), "Registered", null, new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd") },
                        { new Guid("80000004-0000-0000-0000-000000000004"), new DateTime(2026, 6, 16, 9, 30, 0, 0, DateTimeKind.Utc), new Guid("88888888-8888-8888-8888-888888888888"), new DateTime(2026, 6, 16, 9, 30, 0, 0, DateTimeKind.Utc), "Cancelled", new DateTime(2026, 6, 17, 10, 0, 0, 0, DateTimeKind.Utc), new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee") },
                        { new Guid("80000005-0000-0000-0000-000000000005"), new DateTime(2026, 6, 16, 10, 0, 0, 0, DateTimeKind.Utc), new Guid("88888888-8888-8888-8888-888888888888"), new DateTime(2026, 6, 16, 10, 0, 0, 0, DateTimeKind.Utc), "Registered", null, new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff") },
                        { new Guid("80000006-0000-0000-0000-000000000006"), new DateTime(2026, 6, 16, 10, 30, 0, 0, DateTimeKind.Utc), new Guid("88888888-8888-8888-8888-888888888888"), new DateTime(2026, 6, 16, 10, 30, 0, 0, DateTimeKind.Utc), "Registered", null, new Guid("10101010-1010-1010-1010-101010101010") }
                    });
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            if (string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase))
            {
                migrationBuilder.DeleteData(
                    table: "Events",
                    keyColumn: "Id",
                    keyValue: new Guid("11111111-1111-1111-1111-111111111111"));

                migrationBuilder.DeleteData(
                    table: "Events",
                    keyColumn: "Id",
                    keyValue: new Guid("99999999-9999-9999-9999-999999999999"));

                migrationBuilder.DeleteData(
                    table: "Events",
                    keyColumn: "Id",
                    keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

                migrationBuilder.DeleteData(
                    table: "Registrations",
                    keyColumn: "Id",
                    keyValue: new Guid("20000001-0000-0000-0000-000000000001"));

                migrationBuilder.DeleteData(
                    table: "Registrations",
                    keyColumn: "Id",
                    keyValue: new Guid("20000002-0000-0000-0000-000000000002"));

                migrationBuilder.DeleteData(
                    table: "Registrations",
                    keyColumn: "Id",
                    keyValue: new Guid("30000001-0000-0000-0000-000000000001"));

                migrationBuilder.DeleteData(
                    table: "Registrations",
                    keyColumn: "Id",
                    keyValue: new Guid("30000002-0000-0000-0000-000000000002"));

                migrationBuilder.DeleteData(
                    table: "Registrations",
                    keyColumn: "Id",
                    keyValue: new Guid("30000003-0000-0000-0000-000000000003"));

                migrationBuilder.DeleteData(
                    table: "Registrations",
                    keyColumn: "Id",
                    keyValue: new Guid("40000001-0000-0000-0000-000000000001"));

                migrationBuilder.DeleteData(
                    table: "Registrations",
                    keyColumn: "Id",
                    keyValue: new Guid("40000002-0000-0000-0000-000000000002"));

                migrationBuilder.DeleteData(
                    table: "Registrations",
                    keyColumn: "Id",
                    keyValue: new Guid("40000003-0000-0000-0000-000000000003"));

                migrationBuilder.DeleteData(
                    table: "Registrations",
                    keyColumn: "Id",
                    keyValue: new Guid("40000004-0000-0000-0000-000000000004"));

                migrationBuilder.DeleteData(
                    table: "Registrations",
                    keyColumn: "Id",
                    keyValue: new Guid("40000005-0000-0000-0000-000000000005"));

                migrationBuilder.DeleteData(
                    table: "Registrations",
                    keyColumn: "Id",
                    keyValue: new Guid("40000006-0000-0000-0000-000000000006"));

                migrationBuilder.DeleteData(
                    table: "Registrations",
                    keyColumn: "Id",
                    keyValue: new Guid("40000007-0000-0000-0000-000000000007"));

                migrationBuilder.DeleteData(
                    table: "Registrations",
                    keyColumn: "Id",
                    keyValue: new Guid("50000001-0000-0000-0000-000000000001"));

                migrationBuilder.DeleteData(
                    table: "Registrations",
                    keyColumn: "Id",
                    keyValue: new Guid("50000002-0000-0000-0000-000000000002"));

                migrationBuilder.DeleteData(
                    table: "Registrations",
                    keyColumn: "Id",
                    keyValue: new Guid("50000003-0000-0000-0000-000000000003"));

                migrationBuilder.DeleteData(
                    table: "Registrations",
                    keyColumn: "Id",
                    keyValue: new Guid("50000004-0000-0000-0000-000000000004"));

                migrationBuilder.DeleteData(
                    table: "Registrations",
                    keyColumn: "Id",
                    keyValue: new Guid("50000005-0000-0000-0000-000000000005"));

                migrationBuilder.DeleteData(
                    table: "Registrations",
                    keyColumn: "Id",
                    keyValue: new Guid("50000006-0000-0000-0000-000000000006"));

                migrationBuilder.DeleteData(
                    table: "Registrations",
                    keyColumn: "Id",
                    keyValue: new Guid("60000001-0000-0000-0000-000000000001"));

                migrationBuilder.DeleteData(
                    table: "Registrations",
                    keyColumn: "Id",
                    keyValue: new Guid("60000002-0000-0000-0000-000000000002"));

                migrationBuilder.DeleteData(
                    table: "Registrations",
                    keyColumn: "Id",
                    keyValue: new Guid("60000003-0000-0000-0000-000000000003"));

                migrationBuilder.DeleteData(
                    table: "Registrations",
                    keyColumn: "Id",
                    keyValue: new Guid("60000004-0000-0000-0000-000000000004"));

                migrationBuilder.DeleteData(
                    table: "Registrations",
                    keyColumn: "Id",
                    keyValue: new Guid("60000005-0000-0000-0000-000000000005"));

                migrationBuilder.DeleteData(
                    table: "Registrations",
                    keyColumn: "Id",
                    keyValue: new Guid("60000006-0000-0000-0000-000000000006"));

                migrationBuilder.DeleteData(
                    table: "Registrations",
                    keyColumn: "Id",
                    keyValue: new Guid("70000001-0000-0000-0000-000000000001"));

                migrationBuilder.DeleteData(
                    table: "Registrations",
                    keyColumn: "Id",
                    keyValue: new Guid("70000002-0000-0000-0000-000000000002"));

                migrationBuilder.DeleteData(
                    table: "Registrations",
                    keyColumn: "Id",
                    keyValue: new Guid("70000003-0000-0000-0000-000000000003"));

                migrationBuilder.DeleteData(
                    table: "Registrations",
                    keyColumn: "Id",
                    keyValue: new Guid("70000004-0000-0000-0000-000000000004"));

                migrationBuilder.DeleteData(
                    table: "Registrations",
                    keyColumn: "Id",
                    keyValue: new Guid("70000005-0000-0000-0000-000000000005"));

                migrationBuilder.DeleteData(
                    table: "Registrations",
                    keyColumn: "Id",
                    keyValue: new Guid("70000006-0000-0000-0000-000000000006"));

                migrationBuilder.DeleteData(
                    table: "Registrations",
                    keyColumn: "Id",
                    keyValue: new Guid("80000001-0000-0000-0000-000000000001"));

                migrationBuilder.DeleteData(
                    table: "Registrations",
                    keyColumn: "Id",
                    keyValue: new Guid("80000002-0000-0000-0000-000000000002"));

                migrationBuilder.DeleteData(
                    table: "Registrations",
                    keyColumn: "Id",
                    keyValue: new Guid("80000003-0000-0000-0000-000000000003"));

                migrationBuilder.DeleteData(
                    table: "Registrations",
                    keyColumn: "Id",
                    keyValue: new Guid("80000004-0000-0000-0000-000000000004"));

                migrationBuilder.DeleteData(
                    table: "Registrations",
                    keyColumn: "Id",
                    keyValue: new Guid("80000005-0000-0000-0000-000000000005"));

                migrationBuilder.DeleteData(
                    table: "Registrations",
                    keyColumn: "Id",
                    keyValue: new Guid("80000006-0000-0000-0000-000000000006"));

                migrationBuilder.DeleteData(
                    table: "Events",
                    keyColumn: "Id",
                    keyValue: new Guid("22222222-2222-2222-2222-222222222222"));

                migrationBuilder.DeleteData(
                    table: "Events",
                    keyColumn: "Id",
                    keyValue: new Guid("33333333-3333-3333-3333-333333333333"));

                migrationBuilder.DeleteData(
                    table: "Events",
                    keyColumn: "Id",
                    keyValue: new Guid("44444444-4444-4444-4444-444444444444"));

                migrationBuilder.DeleteData(
                    table: "Events",
                    keyColumn: "Id",
                    keyValue: new Guid("55555555-5555-5555-5555-555555555555"));

                migrationBuilder.DeleteData(
                    table: "Events",
                    keyColumn: "Id",
                    keyValue: new Guid("66666666-6666-6666-6666-666666666666"));

                migrationBuilder.DeleteData(
                    table: "Events",
                    keyColumn: "Id",
                    keyValue: new Guid("77777777-7777-7777-7777-777777777777"));

                migrationBuilder.DeleteData(
                    table: "Events",
                    keyColumn: "Id",
                    keyValue: new Guid("88888888-8888-8888-8888-888888888888"));

                migrationBuilder.DeleteData(
                    table: "Users",
                    keyColumn: "Id",
                    keyValue: new Guid("10101010-1010-1010-1010-101010101010"));

                migrationBuilder.DeleteData(
                    table: "Users",
                    keyColumn: "Id",
                    keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

                migrationBuilder.DeleteData(
                    table: "Users",
                    keyColumn: "Id",
                    keyValue: new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"));

                migrationBuilder.DeleteData(
                    table: "Users",
                    keyColumn: "Id",
                    keyValue: new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"));

                migrationBuilder.DeleteData(
                    table: "Users",
                    keyColumn: "Id",
                    keyValue: new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"));

                migrationBuilder.DeleteData(
                    table: "Users",
                    keyColumn: "Id",
                    keyValue: new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"));
            }
        }
    }
}
