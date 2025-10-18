using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZoraVault.Migrations
{
    /// <inheritdoc />
    public partial class Removed_LastLogin_from_User_entity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastLogin",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastLogin",
                table: "Users",
                type: "datetime(6)",
                nullable: true);
        }
    }
}
