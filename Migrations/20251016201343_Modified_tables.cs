using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZoraVault.Migrations
{
    /// <inheritdoc />
    public partial class Modified_tables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "Sessions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "Sessions",
                type: "datetime(6)",
                nullable: true);
        }
    }
}
