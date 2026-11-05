using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Comedor.Api.Tests.Persistencia;
using Comedor.Application.Abstracciones;
using Comedor.Application.Usuarios;
using Comedor.Domain.Accesos;
using Comedor.Infrastructure.Persistencia;
using Microsoft.Extensions.DependencyInjection;

namespace Comedor.Api.Tests;

[Collection("SqlServer")]
public class AdminTests(SqlServerFixture sql)
{
    private sealed record RespuestaToken(string Token);

    private sealed record Err(string Error);

    private static async Task<HttpClient> ClienteAsync(ComedorApiFactory fabrica, string rol, string? usuario = null)
    {
        usuario ??= rol.ToLowerInvariant() + "-admin";
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
    public async Task Admin_ConRolSupervisor_403()
    {
        using var fabrica = new ComedorApiFactory(sql);
        await fabrica.SembrarAsync();
        var c = await ClienteAsync(fabrica, Roles.Supervisor);

        Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync("/api/admin/reglas")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync("/api/admin/usuarios")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.PostAsync("/api/admin/sincronizacion", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.PostAsJsonAsync("/api/admin/contingencia", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await fabrica.CreateClient().GetAsync("/api/admin/puntos")).StatusCode);
    }

    [Fact]
    public async Task Reglas_TraslapeDevuelve400ConTexto_YEliminarInexistente404()
    {
        using var fabrica = new ComedorApiFactory(sql);
        await fabrica.SembrarAsync();
        var c = await ClienteAsync(fabrica, Roles.Administrador);

        var traslape = await c.PutAsJsonAsync("/api/admin/reglas", new
        {
            turno = "Matutino", tiempoComida = "Cena", inicio = "14:00", fin = "16:00", perteneceAJornadaAnterior = false,
        });
        var igual = await c.PutAsJsonAsync("/api/admin/reglas", new
        {
            turno = "Matutino", tiempoComida = "Cena", inicio = "16:00", fin = "16:00", perteneceAJornadaAnterior = false,
        });
        var cruce = await c.PutAsJsonAsync("/api/admin/reglas", new
        {
            turno = "Nocturno", tiempoComida = "Cena", inicio = "23:00", fin = "01:00", perteneceAJornadaAnterior = false,
        });

        Assert.Equal(HttpStatusCode.BadRequest, traslape.StatusCode);
        Assert.Equal("Se traslapa con Comida de 13:00 a 15:00", (await traslape.Content.ReadFromJsonAsync<Err>())!.Error);
        Assert.Equal("El inicio y el fin no pueden ser iguales", (await igual.Content.ReadFromJsonAsync<Err>())!.Error);
        Assert.Equal(HttpStatusCode.OK, cruce.StatusCode);
        var lista = await c.GetStringAsync("/api/admin/reglas");
        Assert.Contains("\"inicio\":\"23:00\"", lista);
        Assert.Equal(HttpStatusCode.NoContent,
            (await c.DeleteAsync("/api/admin/reglas?turno=Nocturno&tiempoComida=Cena")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await c.DeleteAsync("/api/admin/reglas?turno=Nocturno&tiempoComida=Cena")).StatusCode);
    }

    [Fact]
    public async Task Usuario_ContrasenaNoSeDevuelve()
    {
        using var fabrica = new ComedorApiFactory(sql);
        var c = await ClienteAsync(fabrica, Roles.Administrador);

        var alta = await c.PutAsJsonAsync("/api/admin/usuarios",
            new { usuario = "chef1", rol = "Encargado", activo = true, contrasena = "Secreta-123-xyz" });
        var cuerpo = await c.GetStringAsync("/api/admin/usuarios");

        Assert.Equal(HttpStatusCode.OK, alta.StatusCode);
        Assert.Contains("chef1", cuerpo);
        Assert.DoesNotContain("Secreta-123-xyz", cuerpo);
        Assert.DoesNotContain("contrasena", cuerpo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hash", cuerpo, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Usuario_LoginConContrasenaNueva_Funciona()
    {
        using var fabrica = new ComedorApiFactory(sql);
        var c = await ClienteAsync(fabrica, Roles.Administrador);
        await c.PutAsJsonAsync("/api/admin/usuarios",
            new { usuario = "chef1", rol = "Encargado", activo = true, contrasena = "Primera-123-xyz" });

        var sinContrasena = await c.PutAsJsonAsync("/api/admin/usuarios",
            new { usuario = "chef1", rol = "Supervisor", activo = true });
        var login1 = await fabrica.CreateClient().PostAsJsonAsync("/api/auth/login",
            new { usuario = "chef1", contrasena = "Primera-123-xyz" });
        await c.PutAsJsonAsync("/api/admin/usuarios",
            new { usuario = "chef1", rol = "Supervisor", activo = true, contrasena = "Segunda-123-xyz" });
        var loginNueva = await fabrica.CreateClient().PostAsJsonAsync("/api/auth/login",
            new { usuario = "chef1", contrasena = "Segunda-123-xyz" });
        var loginVieja = await fabrica.CreateClient().PostAsJsonAsync("/api/auth/login",
            new { usuario = "chef1", contrasena = "Primera-123-xyz" });

        Assert.Equal(HttpStatusCode.OK, sinContrasena.StatusCode);
        Assert.Equal(HttpStatusCode.OK, login1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, loginNueva.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, loginVieja.StatusCode);
    }

    [Fact]
    public async Task Usuario_RolInvalidoOSinContrasenaAlCrear_400()
    {
        using var fabrica = new ComedorApiFactory(sql);
        var c = await ClienteAsync(fabrica, Roles.Administrador);

        var rol = await c.PutAsJsonAsync("/api/admin/usuarios",
            new { usuario = "x", rol = "Empleado", activo = true, contrasena = "Clave-123-xyz" });
        var sinClave = await c.PutAsJsonAsync("/api/admin/usuarios", new { usuario = "y", rol = "Punto", activo = true });

        Assert.Equal(HttpStatusCode.BadRequest, rol.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, sinClave.StatusCode);
    }

    [Fact]
    public async Task Puntos_AltaYBaja()
    {
        using var fabrica = new ComedorApiFactory(sql);
        await fabrica.SembrarAsync();
        var c = await ClienteAsync(fabrica, Roles.Administrador);

        Assert.Equal(HttpStatusCode.OK,
            (await c.PutAsJsonAsync("/api/admin/puntos", new { id = "P2", nombre = "Entrada 2", activo = true })).StatusCode);
        await c.PutAsJsonAsync("/api/admin/puntos", new { id = "P2", nombre = "Entrada 2", activo = false });

        var cuerpo = await c.GetStringAsync("/api/admin/puntos");
        Assert.Contains("\"id\":\"P2\",\"nombre\":\"Entrada 2\",\"activo\":false", cuerpo);
    }

    [Fact]
    public async Task Sincronizacion_EjecutaYListaBitacora()
    {
        using var fabrica = new ComedorApiFactory(sql);
        var c = await ClienteAsync(fabrica, Roles.Administrador);

        var r = await c.PostAsync("/api/admin/sincronizacion", null);
        var bitacora = await c.GetStringAsync("/api/admin/sincronizacion");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Contains("\"exito\":true", await r.Content.ReadAsStringAsync());
        Assert.Contains("\"exito\":true", bitacora);
    }

    [Fact]
    public async Task Contingencia_RegistraCanalContingencia()
    {
        using var fabrica = new ComedorApiFactory(sql);
        await fabrica.SembrarAsync();
        var c = await ClienteAsync(fabrica, Roles.Administrador, "admin-conti");

        // Sábado: sin derecho por día de descanso, pero la contingencia solo aplica la restricción única.
        var r = await c.PostAsJsonAsync("/api/admin/contingencia", new
        {
            matricula = " e1001 ", fechaServicio = "2026-11-07", tiempoComida = "Comida", referenciaLista = "Lista-0042",
        });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        using var scope = fabrica.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ComedorDbContext>();
        var consumo = Assert.Single(db.Set<Consumo>().Where(x => x.Matricula == "E1001").ToList());
        Assert.Equal(Canal.Contingencia, consumo.Canal);
        Assert.Equal(new DateOnly(2026, 11, 7), consumo.FechaServicio);
        Assert.Equal("admin-conti", consumo.Encargado);
        Assert.Equal("Lista-0042", consumo.Referencia);
        Assert.Equal(ComedorApiFactory.PuntoId, consumo.PuntoId);
        Assert.Equal(fabrica.Reloj.Valor, consumo.FechaHora);
    }

    [Fact]
    public async Task Contingencia_Duplicado_409()
    {
        using var fabrica = new ComedorApiFactory(sql);
        await fabrica.SembrarAsync();
        var c = await ClienteAsync(fabrica, Roles.Administrador);
        var cuerpo = new
        {
            matricula = "E1001", fechaServicio = "2026-11-09", tiempoComida = "Comida", referenciaLista = "Lista-1",
        };
        Assert.Equal(HttpStatusCode.OK, (await c.PostAsJsonAsync("/api/admin/contingencia", cuerpo)).StatusCode);

        var r = await c.PostAsJsonAsync("/api/admin/contingencia", cuerpo);

        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
        Assert.Equal("Ya consumiste este tiempo de comida", (await r.Content.ReadFromJsonAsync<Err>())!.Error);
    }

    [Fact]
    public async Task Contingencia_MatriculaNoRegistradaOSinPunto_400()
    {
        using var fabrica = new ComedorApiFactory(sql);
        await fabrica.SembrarAsync();
        var c = await ClienteAsync(fabrica, Roles.Administrador);

        var noExiste = await c.PostAsJsonAsync("/api/admin/contingencia", new
        {
            matricula = "ZZZ", fechaServicio = "2026-11-09", tiempoComida = "Comida", referenciaLista = "L",
        });
        await c.PutAsJsonAsync("/api/admin/puntos", new { id = ComedorApiFactory.PuntoId, nombre = "x", activo = false });
        var sinPunto = await c.PostAsJsonAsync("/api/admin/contingencia", new
        {
            matricula = "E1001", fechaServicio = "2026-11-09", tiempoComida = "Comida", referenciaLista = "L",
        });

        Assert.Equal(HttpStatusCode.BadRequest, noExiste.StatusCode);
        Assert.Equal("No registrado", (await noExiste.Content.ReadFromJsonAsync<Err>())!.Error);
        Assert.Equal(HttpStatusCode.BadRequest, sinPunto.StatusCode);
        Assert.Equal("No hay un punto de entrada activo", (await sinPunto.Content.ReadFromJsonAsync<Err>())!.Error);
    }
}
