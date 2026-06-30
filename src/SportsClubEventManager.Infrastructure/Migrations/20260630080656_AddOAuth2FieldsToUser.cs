using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportsClubEventManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOAuth2FieldsToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalProviderId",
                table: "Users",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLoginAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderName",
                table: "Users",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefreshToken",
                table: "Users",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefreshTokenExpiryTime",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_ExternalProviderId",
                table: "Users",
                column: "ExternalProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_ProviderName_ExternalProviderId",
                table: "Users",
                columns: new[] { "ProviderName", "ExternalProviderId" },
                unique: true,
                filter: "[ExternalProviderId] IS NOT NULL AND [ProviderName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Users_RefreshToken",
                table: "Users",
                column: "RefreshToken");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_ExternalProviderId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_ProviderName_ExternalProviderId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_RefreshToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ExternalProviderId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastLoginAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ProviderName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RefreshToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RefreshTokenExpiryTime",
                table: "Users");
        }
    }
}
