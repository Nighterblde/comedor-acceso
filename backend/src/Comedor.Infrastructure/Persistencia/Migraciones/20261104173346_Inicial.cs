using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Comedor.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Consumos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Matricula = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechaServicio = table.Column<DateOnly>(type: "date", nullable: false),
                    TiempoComida = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FechaHora = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PuntoId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Canal = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Encargado = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Referencia = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Consumos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Empleados",
                columns: table => new
                {
                    Matricula = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Area = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Turno = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DiasLaborales = table.Column<int>(type: "int", nullable: false),
                    InicioContrato = table.Column<DateOnly>(type: "date", nullable: false),
                    FinContrato = table.Column<DateOnly>(type: "date", nullable: false),
                    FotoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empleados", x => x.Matricula);
                });

            migrationBuilder.CreateTable(
                name: "Entregas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConsumoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaHora = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Usuario = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entregas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Puntos",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Puntos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rechazos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Matricula = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechaHora = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaServicio = table.Column<DateOnly>(type: "date", nullable: true),
                    TiempoComida = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PuntoId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Canal = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Encargado = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Motivo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rechazos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Reglas",
                columns: table => new
                {
                    Turno = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TiempoComida = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Inicio = table.Column<TimeOnly>(type: "time", nullable: false),
                    Fin = table.Column<TimeOnly>(type: "time", nullable: false),
                    PerteneceAJornadaAnterior = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reglas", x => new { x.Turno, x.TiempoComida });
                });

            migrationBuilder.CreateIndex(
                name: "IX_Consumos_Matricula_FechaServicio_TiempoComida",
                table: "Consumos",
                columns: new[] { "Matricula", "FechaServicio", "TiempoComida" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Consumos_PuntoId_FechaHora",
                table: "Consumos",
                columns: new[] { "PuntoId", "FechaHora" });

            migrationBuilder.CreateIndex(
                name: "IX_Entregas_ConsumoId",
                table: "Entregas",
                column: "ConsumoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rechazos_PuntoId_FechaHora",
                table: "Rechazos",
                columns: new[] { "PuntoId", "FechaHora" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Consumos");

            migrationBuilder.DropTable(
                name: "Empleados");

            migrationBuilder.DropTable(
                name: "Entregas");

            migrationBuilder.DropTable(
                name: "Puntos");

            migrationBuilder.DropTable(
                name: "Rechazos");

            migrationBuilder.DropTable(
                name: "Reglas");
        }
    }
}
