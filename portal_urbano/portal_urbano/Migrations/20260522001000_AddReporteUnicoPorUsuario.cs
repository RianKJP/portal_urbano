using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace portal_urbano.Migrations
{
    /// <inheritdoc />
    public partial class AddReporteUnicoPorUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Reportes_IdDenuncia_IdUsuario",
                table: "Reportes",
                columns: new[] { "IdDenuncia", "IdUsuario" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reportes_IdDenuncia_IdUsuario",
                table: "Reportes");
        }
    }
}
