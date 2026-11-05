using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Comedor.Api.Tests.Persistencia;
using Comedor.Application.Abstracciones;
using Comedor.Application.Reportes;
using Comedor.Application.Usuarios;
using Comedor.Domain.Accesos;
using Comedor.Domain.Empleados;
using ClosedXML.Excel;
using Microsoft.Extensions.DependencyInjection;

namespace Comedor.Api.Tests;

[Collection("SqlServer")]
public class ReportesTests(SqlServerFixture sql)
{
    private static readonly DateOnly Dia = new(2026, 11, 10);
    private const string Rango = "desde=2026-11-10&hasta=2026-11-10";

    private sealed record RespuestaToken(string Token);

    private sealed record DetalleJson(DateTime FechaHora, string Matricula, string Nombre, string TiempoComida,
        string PuntoId, string Canal, string Resultado, string? Motivo, bool Servido);

    private static Empleado Emp(string matricula, string nombre, string area, string turno) =>
        new(matricula, nombre, area, turno, new HashSet<DayOfWeek>(), new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31), null, true);

    /// <summary>
    /// 2026-11-10: servidos Comida (E1001 celular, E1002 manual) y Cena (E1003, E1004 N-Cena a las 00:15 del 11-11),
    /// un Comida sin servir (E1003) y un rechazo de E1002.
    /// </summary>
    private async Task<ComedorApiFactory> CrearFabricaSembradaAsync()
    {
        var fabrica = new ComedorApiFactory(sql);
        await fabrica.SembrarAsync();
        using var scope = fabrica.Services.CreateScope();
        var sp = scope.ServiceProvider;
        await sp.GetRequiredService<IEmpleadoRepositorio>().GuardarAsync([
            Emp("E1002", "Beto Ruiz", "Empaque", "Matutino"),
            Emp("E1003", "Carla Soto", "Producción", "Vespertino"),
            Emp("E1004", "Diego Vela", "Empaque", "Nocturno")]);
        var repo = sp.GetRequiredService<IAccesoRepositorio>();
        var p = ComedorApiFactory.PuntoId;
        var hora = new DateTime(2026, 11, 10, 13, 30, 0);

        async Task Servido(string mat, string tiempo, DateTime fh, Canal canal)
        {
            var c = Consumo.Crear(mat, Dia, tiempo, fh, p, canal, null);
            await repo.RegistrarConsumoAsync(c);
            await repo.RegistrarEntregaAsync(Entrega.Crear(c.Id, fh.AddMinutes(1), "punto"));
        }

        await Servido("E1001", "Comida", hora, Canal.Celular);
        await Servido("E1002", "Comida", hora, Canal.Manual);
        await Servido("E1003", "Cena", new DateTime(2026, 11, 10, 20, 0, 0), Canal.Celular);
        await Servido("E1004", "Cena", new DateTime(2026, 11, 11, 0, 15, 0), Canal.Celular);
        await repo.RegistrarConsumoAsync(Consumo.Crear("E1003", Dia, "Comida", hora, p, Canal.Celular, null));
        await repo.RegistrarRechazoAsync(Rechazo.Crear("E1002", hora.AddMinutes(5), Dia, "Comida", p,
            Canal.Celular, null, MotivoRechazo.YaConsumio));
        return fabrica;
    }

    private static async Task<HttpClient> ClienteAsync(ComedorApiFactory fabrica, string rol)
    {
        var usuario = rol.ToLowerInvariant() + "-reportes";
        await fabrica.CrearUsuarioAsync(usuario, "Clave-Prueba-123", rol);
        var r = await fabrica.CreateClient().PostAsJsonAsync("/api/auth/login",
            new { usuario, contrasena = "Clave-Prueba-123" });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var token = (await r.Content.ReadFromJsonAsync<RespuestaToken>())!.Token;
        var c = fabrica.CreateClient();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return c;
    }

    [Fact]
    public async Task Cotejo_AgrupaPorFechaServicio()
    {
        using var fabrica = await CrearFabricaSembradaAsync();
        var c = await ClienteAsync(fabrica, Roles.Supervisor);

        var filas = (await c.GetFromJsonAsync<FilaCotejo[]>($"/api/reportes/cotejo?{Rango}"))!;

        Assert.Equal(2, filas.Length);
        var comida = filas.Single(f => f.TiempoComida == "Comida");
        Assert.Equal(Dia, comida.FechaServicio);
        Assert.Equal(2, comida.Servidas);
        Assert.Equal(1, comida.SinServir);
        var cena = filas.Single(f => f.TiempoComida == "Cena");
        Assert.Equal(Dia, cena.FechaServicio);
        Assert.Equal(2, cena.Servidas);
        Assert.Equal(0, cena.SinServir);
    }

    [Fact]
    public async Task Cotejo_ExcluyeRechazos()
    {
        using var fabrica = await CrearFabricaSembradaAsync();
        var c = await ClienteAsync(fabrica, Roles.Administrador);

        var filas = (await c.GetFromJsonAsync<FilaCotejo[]>($"/api/reportes/cotejo?{Rango}"))!;

        Assert.Equal(5, filas.Sum(f => f.Servidas + f.SinServir));
    }

    [Fact]
    public async Task Cotejo_ContingenciaCuentaComoServida()
    {
        using var fabrica = await CrearFabricaSembradaAsync();
        using (var scope = fabrica.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IAccesoRepositorio>().RegistrarConsumoAsync(
                Consumo.Crear("E1001", new DateOnly(2026, 11, 12), "Comida", new DateTime(2026, 11, 12, 13, 30, 0),
                    ComedorApiFactory.PuntoId, Canal.Contingencia, "enc", "folio-1"));
        }

        var c = await ClienteAsync(fabrica, Roles.Supervisor);

        var filas = (await c.GetFromJsonAsync<FilaCotejo[]>("/api/reportes/cotejo?desde=2026-11-12&hasta=2026-11-12"))!;

        var fila = Assert.Single(filas);
        Assert.Equal(1, fila.Servidas);
        Assert.Equal(0, fila.SinServir);
    }

    [Fact]
    public async Task Conteo_AgrupaPorTurnoYArea()
    {
        using var fabrica = await CrearFabricaSembradaAsync();
        var c = await ClienteAsync(fabrica, Roles.Supervisor);

        var filas = (await c.GetFromJsonAsync<FilaConteo[]>($"/api/reportes/conteo?{Rango}"))!;

        Assert.Equal(5, filas.Length);
        Assert.Equal(5, filas.Sum(f => f.Total));
        Assert.Single(filas, f => f is { TiempoComida: "Comida", Turno: "Matutino", Area: "Producción", Total: 1 });
        Assert.Single(filas, f => f is { TiempoComida: "Cena", Turno: "Nocturno", Area: "Empaque", Total: 1 });
        Assert.Single(filas, f => f is { TiempoComida: "Comida", Turno: "Vespertino", Area: "Producción", Total: 1 });
    }

    [Fact]
    public async Task CotejoExcel_ReleidoConClosedXML_TotalServidasEs4()
    {
        using var fabrica = await CrearFabricaSembradaAsync();
        var c = await ClienteAsync(fabrica, Roles.Supervisor);

        var r = await c.GetAsync($"/api/reportes/cotejo.xlsx?{Rango}");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", r.Content.Headers.ContentType!.MediaType);
        Assert.Equal("cotejo-2026-11-10_2026-11-10.xlsx", r.Content.Headers.ContentDisposition!.FileName);
        using var libro = new XLWorkbook(await r.Content.ReadAsStreamAsync());
        var ultima = libro.Worksheet(1).LastRowUsed()!;
        Assert.Equal("Total", ultima.Cell(1).GetString());
        Assert.Equal(4, (int)ultima.Cell(3).GetDouble());
        Assert.Equal(1, (int)ultima.Cell(4).GetDouble());
    }

    [Fact]
    public async Task CotejoPdf_EmpiezaConFirmaPdf()
    {
        using var fabrica = await CrearFabricaSembradaAsync();
        var c = await ClienteAsync(fabrica, Roles.Supervisor);

        var r = await c.GetAsync($"/api/reportes/cotejo.pdf?{Rango}");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal("application/pdf", r.Content.Headers.ContentType!.MediaType);
        var bytes = await r.Content.ReadAsByteArrayAsync();
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
    }

    [Fact]
    public async Task Detalle_FiltraPorCanal()
    {
        using var fabrica = await CrearFabricaSembradaAsync();
        var c = await ClienteAsync(fabrica, Roles.Supervisor);

        var todas = (await c.GetFromJsonAsync<DetalleJson[]>($"/api/reportes/detalle?{Rango}"))!;
        var manual = (await c.GetFromJsonAsync<DetalleJson[]>($"/api/reportes/detalle?{Rango}&canal=Manual"))!;
        var rechazos = (await c.GetFromJsonAsync<DetalleJson[]>($"/api/reportes/detalle?{Rango}&motivo=YaConsumio"))!;

        Assert.Equal(6, todas.Length);
        var fila = Assert.Single(manual);
        Assert.Equal("E1002", fila.Matricula);
        Assert.Equal("Beto Ruiz", fila.Nombre);
        Assert.Equal("Autorizado", fila.Resultado);
        Assert.True(fila.Servido);
        var rechazo = Assert.Single(rechazos);
        Assert.Equal("Rechazado", rechazo.Resultado);
        Assert.Equal("Ya consumiste este tiempo de comida", rechazo.Motivo);
        Assert.False(rechazo.Servido);
        Assert.Equal(todas.OrderByDescending(f => f.FechaHora).Select(f => f.FechaHora), todas.Select(f => f.FechaHora));
    }

    [Fact]
    public async Task DetalleExcel_DevuelveArchivo()
    {
        using var fabrica = await CrearFabricaSembradaAsync();
        var c = await ClienteAsync(fabrica, Roles.Supervisor);

        var r = await c.GetAsync($"/api/reportes/detalle.xlsx?{Rango}");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        using var libro = new XLWorkbook(await r.Content.ReadAsStreamAsync());
        Assert.Equal(7, libro.Worksheet(1).LastRowUsed()!.RowNumber());
    }

    [Fact]
    public async Task Reportes_FechasInvalidas_400()
    {
        using var fabrica = await CrearFabricaSembradaAsync();
        var c = await ClienteAsync(fabrica, Roles.Supervisor);

        Assert.Equal(HttpStatusCode.BadRequest, (await c.GetAsync("/api/reportes/cotejo?desde=hoy&hasta=2026-11-10")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.GetAsync("/api/reportes/conteo")).StatusCode);
    }

    [Fact]
    public async Task Reportes_ConRolEncargado_403()
    {
        using var fabrica = await CrearFabricaSembradaAsync();
        var c = await ClienteAsync(fabrica, Roles.Encargado);

        Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync($"/api/reportes/cotejo?{Rango}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync($"/api/reportes/detalle.xlsx?{Rango}")).StatusCode);
    }
}
