using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Comedor.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class UsuariosSistema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UsuariosSistema",
                columns: table => new
                {
                    Usuario = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    HashContrasena = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Rol = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuariosSistema", x => x.Usuario);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UsuariosSistema");
        }
    }
}
