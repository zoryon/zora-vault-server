using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZoraVault.Migrations
{
    /// <inheritdoc />
    public partial class Remove_columns_from_UserSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowScreenCapture",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "ClipboardClearDelaySeconds",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "EnableAccessibility",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "EnableAutoFill",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "EnableClipboardClearing",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "UnlockWithBiometrics",
                table: "UserSettings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowScreenCapture",
                table: "UserSettings",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ClipboardClearDelaySeconds",
                table: "UserSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "EnableAccessibility",
                table: "UserSettings",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EnableAutoFill",
                table: "UserSettings",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EnableClipboardClearing",
                table: "UserSettings",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "UnlockWithBiometrics",
                table: "UserSettings",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }
    }
}
