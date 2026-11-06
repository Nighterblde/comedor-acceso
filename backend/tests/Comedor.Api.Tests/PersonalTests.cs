using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ClosedXML.Excel;
using Comedor.Api.Tests.Persistencia;
using Comedor.Application.Abstracciones;
using Comedor.Application.Usuarios;
using Microsoft.Extensions.DependencyInjection;

namespace Comedor.Api.Tests;

[Collection("SqlServer")]
public sealed class PersonalTests(SqlServerFixture sql) : IDisposable
{
    private sealed record RespuestaToken(string Token);

    private sealed record Err(string Error);

    private sealed record Resultado(bool Exito, int Altas, int Cambios, int Bajas, int Omitidos, string? Error);

    private readonly string _dir = Path.Combine(Path.GetTempPath(), "comedor-personal-" + Guid.NewGuid().ToString("N"));

    private string Ruta => Path.Combine(_dir, "personal.xlsx");

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, true);
        }
    }

    private ComedorApiFactory Fabrica() => new(sql) { RutaExcel = Ruta };

    private static async Task<HttpClient> ClienteAsync(ComedorApiFactory fabrica, string rol)
    {
        var usuario = rol.ToLowerInvariant() + "-personal";
        await fabrica.CrearUsuarioAsync(usuario, "Clave-Prueba-123", rol);
        var r = await fabrica.CreateClient().PostAsJsonAsync("/api/auth/login",
            new { usuario, contrasena = "Clave-Prueba-123" });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var token = (await r.Content.ReadFromJsonAsync<RespuestaToken>())!.Token;
        var c = fabrica.CreateClient();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return c;
    }

    private static byte[] Libro(Action<IXLWorksheet> llenar)
    {
        using var libro = new XLWorkbook();
        var h = libro.AddWorksheet("Personal");
        string[] enc = ["Matricula", "Nombre", "Area", "Turno", "DiasLaborales", "InicioContrato", "FinContrato", "FotoUrl"];
        for (var i = 0; i < enc.Length; i++)
        {
            h.Cell(1, i + 1).Value = enc[i];
        }

        llenar(h);
        using var ms = new MemoryStream();
        libro.SaveAs(ms);
        return ms.ToArray();
    }

    private static void Fila(IXLWorksheet h, int n, string matricula, string area, string inicio)
    {
        h.Cell(n, 1).Value = matricula;
        h.Cell(n, 2).Value = "Nombre " + matricula;
        h.Cell(n, 3).Value = area;
        h.Cell(n, 4).Value = "Matutino";
        h.Cell(n, 5).Value = "L,M,X,J,V";
        h.Cell(n, 6).Value = inicio;
        h.Cell(n, 7).Value = new DateTime(2026, 12, 31);
    }

    private static MultipartFormDataContent Formulario(byte[] bytes, string nombre)
    {
        var contenido = new ByteArrayContent(bytes);
        contenido.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        return new MultipartFormDataContent { { contenido, "archivo", nombre } };
    }

    [Fact]
    public async Task Importar_Xlsx_SincronizaYDevuelveResultado()
    {
        using var fabrica = Fabrica();
        await fabrica.SembrarAsync(); // E1001 "Producción", contrato desde 2026-01-01
        var c = await ClienteAsync(fabrica, Roles.Administrador);
        var bytes = Libro(h =>
        {
            Fila(h, 2, "E1001", "Cocina", "2026-01-01"); // cambio
            Fila(h, 3, "N2002", "Almacén", "2026-01-01"); // alta
            Fila(h, 4, "N2003", "Almacén", "fecha mala"); // omitida
        });

        var r = await c.PostAsync("/api/admin/personal/importar", Formulario(bytes, "personal.xlsx"));

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var res = (await r.Content.ReadFromJsonAsync<Resultado>())!;
        Assert.Equal(new Resultado(true, 1, 1, 0, 1, null), res);
        Assert.True(File.Exists(Ruta));
        using var scope = fabrica.Services.CreateScope();
        var empleados = await scope.ServiceProvider.GetRequiredService<IEmpleadoRepositorio>().TodosAsync();
        Assert.Contains(empleados, e => e.Matricula == "N2002" && e.Activo);
        Assert.Equal("Cocina", empleados.Single(e => e.Matricula == "E1001").Area);
    }

    [Fact]
    public async Task Importar_NoXlsx_400()
    {
        using var fabrica = Fabrica();
        var c = await ClienteAsync(fabrica, Roles.Administrador);
        var xlsx = Libro(_ => { });

        var porExtension = await c.PostAsync("/api/admin/personal/importar", Formulario(xlsx, "personal.csv"));
        var porContenido = await c.PostAsync("/api/admin/personal/importar",
            Formulario("Matricula,Nombre"u8.ToArray(), "personal.xlsx"));
        var grande = await c.PostAsync("/api/admin/personal/importar",
            Formulario(new byte[5 * 1024 * 1024 + 1], "personal.xlsx"));
        var sinArchivo = await c.PostAsync("/api/admin/personal/importar", new MultipartFormDataContent());

        foreach (var r in new[] { porExtension, porContenido, grande, sinArchivo })
        {
            Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
            Assert.Equal("Sube un archivo .xlsx de hasta 5 MB", (await r.Content.ReadFromJsonAsync<Err>())!.Error);
        }

        Assert.False(File.Exists(Ruta));
    }

    [Fact]
    public async Task Importar_XlsxCorrupto_NoReemplazaElArchivoAnterior()
    {
        using var fabrica = Fabrica();
        await fabrica.SembrarAsync();
        var c = await ClienteAsync(fabrica, Roles.Administrador);
        var bueno = Libro(h => Fila(h, 2, "E1001", "Cocina", "2026-01-01"));
        await c.PostAsync("/api/admin/personal/importar", Formulario(bueno, "personal.xlsx"));
        var antes = await File.ReadAllBytesAsync(Ruta);
        byte[] corrupto = [(byte)'P', (byte)'K', 1, 2, 3, 4, 5];

        var r = await c.PostAsync("/api/admin/personal/importar", Formulario(corrupto, "personal.xlsx"));

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.False((await r.Content.ReadFromJsonAsync<Resultado>())!.Exito);
        Assert.Equal(antes, await File.ReadAllBytesAsync(Ruta));
        Assert.Equal([Ruta], Directory.GetFiles(_dir));
    }

    [Fact]
    public async Task Importar_ConRolChef_403()
    {
        using var fabrica = Fabrica();
        await fabrica.SembrarAsync(); // el login del Chef exige un punto activo
        var c = await ClienteAsync(fabrica, Roles.Chef);

        var importar = await c.PostAsync("/api/admin/personal/importar", Formulario(Libro(_ => { }), "personal.xlsx"));
        var plantilla = await c.GetAsync("/api/admin/personal/plantilla.xlsx");

        Assert.Equal(HttpStatusCode.Forbidden, importar.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, plantilla.StatusCode);
    }

    [Fact]
    public async Task Plantilla_DevuelveXlsxConEncabezados()
    {
        using var fabrica = Fabrica();
        var c = await ClienteAsync(fabrica, Roles.Administrador);

        var r = await c.GetAsync("/api/admin/personal/plantilla.xlsx");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", r.Content.Headers.ContentType!.MediaType);
        using var libro = new XLWorkbook(await r.Content.ReadAsStreamAsync());
        var h = libro.Worksheet(1);
        Assert.Equal(
            ["Matricula", "Nombre", "Area", "Turno", "DiasLaborales", "InicioContrato", "FinContrato", "FotoUrl"],
            Enumerable.Range(1, 8).Select(i => h.Cell(1, i).GetString()));
        Assert.False(h.Cell(2, 1).IsEmpty());
        Assert.True(h.Cell(3, 1).IsEmpty());
    }

    [Fact]
    public async Task Plantilla_ColumnaMatriculaEsTexto()
    {
        using var fabrica = Fabrica();
        var c = await ClienteAsync(fabrica, Roles.Administrador);

        var r = await c.GetAsync("/api/admin/personal/plantilla.xlsx");

        using var libro = new XLWorkbook(await r.Content.ReadAsStreamAsync());
        var h = libro.Worksheet(1);
        Assert.Equal(XLDataType.Text, h.Cell(2, 1).DataType);
        Assert.Equal("@", h.Cell(2, 1).Style.NumberFormat.Format);
        Assert.Equal("@", h.Cell(50, 1).Style.NumberFormat.Format);
    }
}
