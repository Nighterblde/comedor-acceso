using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Comedor.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class TresRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE UsuariosSistema SET Rol = 'Chef' WHERE Rol IN ('Punto', 'Encargado')");
            migrationBuilder.Sql("UPDATE UsuariosSistema SET Rol = 'Administrador' WHERE Rol = 'Supervisor'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Vacío a propósito: no se puede saber el rol original de cada usuario.
        }
    }
}
