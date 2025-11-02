using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZoraVault.Migrations
{
    /// <inheritdoc />
    public partial class Add_deletedAt_field_for_VaultItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "VaultItems",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "VaultItems");
        }
    }
}
