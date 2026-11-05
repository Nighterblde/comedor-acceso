using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Comedor.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class BitacoraSincronizacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BitacoraSincronizacion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Exito = table.Column<bool>(type: "bit", nullable: false),
                    Altas = table.Column<int>(type: "int", nullable: false),
                    Cambios = table.Column<int>(type: "int", nullable: false),
                    Bajas = table.Column<int>(type: "int", nullable: false),
                    Omitidos = table.Column<int>(type: "int", nullable: false),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BitacoraSincronizacion", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BitacoraSincronizacion_Fecha",
                table: "BitacoraSincronizacion",
                column: "Fecha");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BitacoraSincronizacion");
        }
    }
}
