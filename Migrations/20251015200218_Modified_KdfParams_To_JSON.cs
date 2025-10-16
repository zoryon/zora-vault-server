using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZoraVault.Migrations
{
    /// <inheritdoc />
    public partial class Modified_KdfParams_To_JSON : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KdfParams_Algorithm",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "KdfParams_Iterations",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "KdfParams_MemoryKb",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "KdfParams_Parallelism",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "KdfParams_Salt",
                table: "Users",
                newName: "KdfParams");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "KdfParams",
                table: "Users",
                newName: "KdfParams_Salt");

            migrationBuilder.AddColumn<string>(
                name: "KdfParams_Algorithm",
                table: "Users",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "KdfParams_Iterations",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "KdfParams_MemoryKb",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KdfParams_Parallelism",
                table: "Users",
                type: "int",
                nullable: true);
        }
    }
}
