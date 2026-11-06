using ClosedXML.Excel;
using Comedor.Api.Tests.Persistencia;
using Comedor.Infrastructure.Externos;
using Microsoft.Data.SqlClient;

namespace Comedor.Api.Tests;

public class DiasLaboralesTextoTests
{
    [Fact]
    public void DiasLaborales_LunesAViernes()
    {
        var dias = DiasLaboralesTexto.Parse("L,M,X,J,V");

        Assert.Equal(
            new HashSet<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday },
            dias.ToHashSet());
    }

    [Fact]
    public void DiasLaborales_EspaciosYMinusculas()
    {
        var dias = DiasLaboralesTexto.Parse(" l , x,  s ,d ");

        Assert.Equal(
            new HashSet<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Saturday, DayOfWeek.Sunday },
            dias.ToHashSet());
    }

    [Fact]
    public void DiasLaborales_LetraDesconocida_Lanza()
    {
        Assert.Throws<FormatException>(() => DiasLaboralesTexto.Parse("L,Z"));
        Assert.Throws<FormatException>(() => DiasLaboralesTexto.Parse(""));
    }
}

public sealed class FuenteRRHHExcelTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "comedor-xlsx-" + Guid.NewGuid().ToString("N"));

    public FuenteRRHHExcelTests() => Directory.CreateDirectory(_dir);

    public void Dispose() => Directory.Delete(_dir, true);

    private static readonly string[] Encabezados =
        ["Matricula", "Nombre", "Area", "Turno", "DiasLaborales", "InicioContrato", "FinContrato", "FotoUrl"];

    private string Crear(Action<IXLWorksheet> llenar)
    {
        var ruta = Path.Combine(_dir, Guid.NewGuid().ToString("N") + ".xlsx");
        using var libro = new XLWorkbook();
        var hoja = libro.AddWorksheet("Personal");
        for (var i = 0; i < Encabezados.Length; i++)
        {
            hoja.Cell(1, i + 1).Value = Encabezados[i];
        }

        llenar(hoja);
        libro.SaveAs(ruta);
        return ruta;
    }

    [Fact]
    public async Task Excel_LeeEmpleados()
    {
        var ruta = Crear(h =>
        {
            h.Cell(2, 1).Value = "E1001";
            h.Cell(2, 2).Value = "Ana Pérez";
            h.Cell(2, 3).Value = "Producción";
            h.Cell(2, 4).Value = "Matutino";
            h.Cell(2, 5).Value = "L,M,X,J,V";
            h.Cell(2, 6).Value = new DateTime(2026, 1, 1);
            h.Cell(2, 7).Value = new DateTime(2026, 12, 31);
            h.Cell(2, 8).Value = "https://fotos/e1001.jpg";
            h.Cell(3, 1).Value = "E1002";
            h.Cell(3, 2).Value = "Luis Ríos";
            h.Cell(3, 3).Value = "Almacén";
            h.Cell(3, 4).Value = "Nocturno";
            h.Cell(3, 5).Value = "s, d";
            h.Cell(3, 6).Value = new DateTime(2026, 2, 1);
            h.Cell(3, 7).Value = new DateTime(2027, 1, 31);
        });

        var filas = await new FuenteRRHHExcel(ruta).ObtenerEmpleadosAsync();

        Assert.Equal(2, filas.Count);
        var ana = filas[0];
        Assert.Equal("E1001", ana.Matricula);
        Assert.Equal("Ana Pérez", ana.Nombre);
        Assert.Equal("Producción", ana.Area);
        Assert.Equal("Matutino", ana.Turno);
        Assert.Equal(5, ana.DiasLaborales.Count);
        Assert.Equal(new DateOnly(2026, 1, 1), ana.InicioContrato);
        Assert.Equal(new DateOnly(2026, 12, 31), ana.FinContrato);
        Assert.Equal("https://fotos/e1001.jpg", ana.FotoUrl);
        Assert.Null(filas[1].FotoUrl);
        Assert.Equal(new HashSet<DayOfWeek> { DayOfWeek.Saturday, DayOfWeek.Sunday }, filas[1].DiasLaborales.ToHashSet());
    }

    [Fact]
    public async Task Excel_MatriculaNumerica_SeLeeComoEnteroSinFormato()
    {
        var ruta = Crear(h =>
        {
            LlenarFila(h, 2, "x");
            h.Cell(2, 1).Value = 1001;
            LlenarFila(h, 3, "x");
            h.Cell(3, 1).Value = 123456789012345L;
            LlenarFila(h, 4, "x");
            h.Cell(4, 1).Style.NumberFormat.Format = "@";
            h.Cell(4, 1).Value = " 00123 ";
        });

        var filas = await new FuenteRRHHExcel(ruta).ObtenerEmpleadosAsync();

        Assert.Equal(["1001", "123456789012345", "00123"], filas.Select(f => f.Matricula));
    }

    private static void LlenarFila(IXLWorksheet h, int fila, string matricula)
    {
        h.Cell(fila, 1).Value = matricula;
        h.Cell(fila, 2).Value = "Ana";
        h.Cell(fila, 3).Value = "Area";
        h.Cell(fila, 4).Value = "Matutino";
        h.Cell(fila, 5).Value = "L,M";
        h.Cell(fila, 6).Value = new DateTime(2026, 1, 1);
        h.Cell(fila, 7).Value = new DateTime(2026, 12, 31);
    }

    [Fact]
    public async Task Excel_FilaConFechaInvalida_SeOmite()
    {
        var ruta = Crear(h =>
        {
            h.Cell(2, 1).Value = "E1001";
            h.Cell(2, 2).Value = "Ana Pérez";
            h.Cell(2, 3).Value = "Producción";
            h.Cell(2, 4).Value = "Matutino";
            h.Cell(2, 5).Value = "L,M";
            h.Cell(2, 6).Value = "no es fecha";
            h.Cell(2, 7).Value = new DateTime(2026, 12, 31);
            h.Cell(3, 1).Value = "E1002";
            h.Cell(3, 2).Value = "Luis Ríos";
            h.Cell(3, 3).Value = "Almacén";
            h.Cell(3, 4).Value = "Matutino";
            h.Cell(3, 5).Value = "L,Q";
            h.Cell(3, 6).Value = new DateTime(2026, 2, 1);
            h.Cell(3, 7).Value = new DateTime(2027, 1, 31);
            h.Cell(4, 1).Value = "E1003";
            h.Cell(4, 2).Value = "Eva Soto";
            h.Cell(4, 3).Value = "Almacén";
            h.Cell(4, 4).Value = "Matutino";
            h.Cell(4, 5).Value = "L";
            h.Cell(4, 6).Value = new DateTime(2026, 2, 1);
            h.Cell(4, 7).Value = new DateTime(2027, 1, 31);
        });

        var filas = await new FuenteRRHHExcel(ruta).ObtenerEmpleadosAsync();

        // Las filas inválidas llegan con turno vacío: SincronizarPersonal las cuenta como omitidas (D-28).
        var validas = filas.Where(f => f.Turno.Length > 0).ToList();
        Assert.Equal(["E1003"], validas.Select(f => f.Matricula));
        Assert.Equal(["E1001", "E1002"], filas.Where(f => f.Turno.Length == 0).Select(f => f.Matricula));
    }

    [Fact]
    public async Task Excel_SinEncabezados_Lanza()
    {
        var ruta = Path.Combine(_dir, "mal.xlsx");
        using (var libro = new XLWorkbook())
        {
            libro.AddWorksheet("x").Cell(1, 1).Value = "Otra";
            libro.SaveAs(ruta);
        }

        await Assert.ThrowsAsync<InvalidDataException>(() => new FuenteRRHHExcel(ruta).ObtenerEmpleadosAsync());
    }
}

[Collection("SqlServer")]
public class FuenteRRHHSqlServerTests(SqlServerFixture sql)
{
    [Fact]
    public async Task SqlServer_LeeVista()
    {
        await using (var cn = new SqlConnection(sql.CadenaConexion))
        {
            await cn.OpenAsync();
            await using var cmd = cn.CreateCommand();
            cmd.CommandText = """
                IF OBJECT_ID('dbo.vw_EmpleadosComedor') IS NOT NULL DROP VIEW dbo.vw_EmpleadosComedor;
                IF OBJECT_ID('dbo.RRHH_Empleados') IS NOT NULL DROP TABLE dbo.RRHH_Empleados;
                CREATE TABLE dbo.RRHH_Empleados (
                    Clave nvarchar(20), NombreCompleto nvarchar(100), Depto nvarchar(100), Turno nvarchar(20),
                    Dias nvarchar(20), Ingreso date, Termino date, Foto nvarchar(200) NULL);
                INSERT dbo.RRHH_Empleados VALUES
                    ('E1001', N'Ana Pérez', N'Producción', 'Matutino', 'L,M,X,J,V', '2026-01-01', '2026-12-31', 'https://f/1.jpg'),
                    ('E1002', N'Luis Ríos', N'Almacén', 'Nocturno', 'S,D', '2026-02-01', '2027-01-31', NULL);
                """;
            await cmd.ExecuteNonQueryAsync();
            cmd.CommandText = """
                CREATE VIEW dbo.vw_EmpleadosComedor AS
                SELECT Clave AS Matricula, NombreCompleto AS Nombre, Depto AS Area, Turno, Dias AS DiasLaborales,
                       Ingreso AS InicioContrato, Termino AS FinContrato, Foto AS FotoUrl
                FROM dbo.RRHH_Empleados;
                """;
            await cmd.ExecuteNonQueryAsync();
        }

        var filas = await new FuenteRRHHSqlServer(sql.CadenaConexion, "vw_EmpleadosComedor").ObtenerEmpleadosAsync();

        Assert.Equal(2, filas.Count);
        var ana = filas.Single(f => f.Matricula == "E1001");
        Assert.Equal("Ana Pérez", ana.Nombre);
        Assert.Equal("Producción", ana.Area);
        Assert.Equal(5, ana.DiasLaborales.Count);
        Assert.Equal(new DateOnly(2026, 1, 1), ana.InicioContrato);
        Assert.Equal(new DateOnly(2026, 12, 31), ana.FinContrato);
        Assert.Equal("https://f/1.jpg", ana.FotoUrl);
        Assert.Null(filas.Single(f => f.Matricula == "E1002").FotoUrl);
    }

    [Theory]
    [InlineData("vw; DROP TABLE x")]
    [InlineData("vw EmpleadosComedor")]
    [InlineData("")]
    [InlineData("v--")]
    public void SqlServer_VistaConNombreInvalido_Lanza(string vista)
    {
        Assert.Throws<ArgumentException>(() => new FuenteRRHHSqlServer("Server=x", vista));
    }
}
