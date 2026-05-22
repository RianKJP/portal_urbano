using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace portal_urbano.Migrations
{
    /// <inheritdoc />
    public partial class AddModeracaoUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Avisos",
                table: "Usuarios",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "Banido",
                table: "Usuarios",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Avisos",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "Banido",
                table: "Usuarios");
        }
    }
}
