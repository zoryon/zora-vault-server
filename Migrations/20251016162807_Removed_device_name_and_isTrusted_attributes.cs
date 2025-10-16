using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZoraVault.Migrations
{
    /// <inheritdoc />
    public partial class Removed_device_name_and_isTrusted_attributes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeviceName",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "IsTrusted",
                table: "Devices");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeviceName",
                table: "Devices",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "IsTrusted",
                table: "Devices",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }
    }
}
