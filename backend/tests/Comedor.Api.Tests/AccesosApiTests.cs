using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Comedor.Api.Tests.Persistencia;
using Comedor.Application.Qr;
using Comedor.Application.Usuarios;
using Comedor.Infrastructure.Persistencia;
using Comedor.Infrastructure.Seguridad;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Comedor.Api.Tests;

[Collection("SqlServer")]
public class AccesosApiTests(SqlServerFixture sql)
{
    private const string Motivo = "Ya consumiste este tiempo de comida";

    private sealed record RespuestaToken(string Token);

    private sealed record RespuestaQr(string Token, string Url);

    private sealed record Acceso(string Estado, string? Motivo, string? Matricula, string? Nombre);

    private sealed record Solicitud(Guid? ConsumoId, string Matricula, string? Nombre, bool Autorizado, bool Servido);

    private sealed record Servido(bool YaServido, DateTime ServidoEn);

    private async Task<ComedorApiFactory> CrearFabricaAsync()
    {
        var fabrica = new ComedorApiFactory(sql);
        await fabrica.SembrarAsync();
        return fabrica;
    }

    private static HttpClient ConToken(ComedorApiFactory fabrica, string token)
    {
        var c = fabrica.CreateClient();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return c;
    }

    private static async Task<string> TokenEmpleadoAsync(ComedorApiFactory fabrica)
    {
        var r = await fabrica.CreateClient().PostAsJsonAsync("/api/empleados/sesion",
            new { id = ComedorApiFactory.EmpleadoMatricula, contrasena = ComedorApiFactory.EmpleadoContrasena });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<RespuestaToken>())!.Token;
    }

    private static async Task<string> TokenSistemaAsync(ComedorApiFactory fabrica, string rol)
    {
        var usuario = rol.ToLowerInvariant() + "-prueba";
        await fabrica.CrearUsuarioAsync(usuario, "Clave-Prueba-123", rol);
        var r = await fabrica.CreateClient().PostAsJsonAsync("/api/auth/login",
            new { usuario, contrasena = "Clave-Prueba-123" });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<RespuestaToken>())!.Token;
    }

    private static async Task<string> TokenQrAsync(HttpClient punto)
    {
        var r = await punto.GetAsync($"/api/puntos/{ComedorApiFactory.PuntoId}/qr");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<RespuestaQr>())!.Token;
    }

    private static async Task<Acceso> LeerAccesoAsync(HttpResponseMessage r)
    {
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<Acceso>())!;
    }

    private static async Task<Guid> ConsumoIdAsync(HttpClient punto)
    {
        var lista = (await punto.GetFromJsonAsync<Solicitud[]>($"/api/puntos/{ComedorApiFactory.PuntoId}/solicitudes"))!;
        return lista.Single(s => s.Autorizado).ConsumoId!.Value;
    }

    [Fact]
    public async Task AccesoCelular_SinToken_401()
    {
        using var fabrica = await CrearFabricaAsync();

        var r = await fabrica.CreateClient().PostAsJsonAsync("/api/accesos/celular", new { tokenQr = "x" });

        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task AccesoManual_ConRolEmpleado_403()
    {
        using var fabrica = await CrearFabricaAsync();
        var cliente = ConToken(fabrica, await TokenEmpleadoAsync(fabrica));

        var r = await cliente.PostAsJsonAsync("/api/accesos/manual",
            new { matricula = ComedorApiFactory.EmpleadoMatricula, puntoId = ComedorApiFactory.PuntoId });

        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }

    [Fact]
    public async Task AccesoCelular_TokenQrDeOtraClave_EscanearDeNuevo()
    {
        using var fabrica = await CrearFabricaAsync();
        var ajeno = new FirmaTokenHmac("otra-clave-distinta-con-mas-de-32-caracteres")
            .Firmar(new TokenQr(ComedorApiFactory.PuntoId, fabrica.Reloj.Valor));
        var cliente = ConToken(fabrica, await TokenEmpleadoAsync(fabrica));

        var acceso = await LeerAccesoAsync(await cliente.PostAsJsonAsync("/api/accesos/celular", new { tokenQr = ajeno }));

        Assert.Equal("EscanearDeNuevo", acceso.Estado);
        Assert.Equal("Escanea de nuevo el código de la entrada", acceso.Motivo);
    }

    [Fact]
    public async Task AccesoCelular_CuerpoVacio_400()
    {
        using var fabrica = await CrearFabricaAsync();
        var cliente = ConToken(fabrica, await TokenEmpleadoAsync(fabrica));

        var r = await cliente.PostAsJsonAsync("/api/accesos/celular", new { tokenQr = "" });

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task SesionEmpleado_IdNulo_401SinError500()
    {
        using var fabrica = await CrearFabricaAsync();

        var r = await fabrica.CreateClient().PostAsJsonAsync("/api/empleados/sesion", new { id = (string?)null, contrasena = "x" });

        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
        Assert.Contains("CredencialesIncorrectas", await r.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task FlujoCompleto_SesionQrYAcceso_Autorizado()
    {
        using var fabrica = await CrearFabricaAsync();
        var punto = ConToken(fabrica, await TokenSistemaAsync(fabrica, Roles.Chef));
        var qrRespuesta = (await punto.GetFromJsonAsync<RespuestaQr>($"/api/puntos/{ComedorApiFactory.PuntoId}/qr"))!;
        Assert.Equal("http://localhost:4200/pase?t=" + Uri.EscapeDataString(qrRespuesta.Token), qrRespuesta.Url);
        var empleado = ConToken(fabrica, await TokenEmpleadoAsync(fabrica));

        var acceso = await LeerAccesoAsync(
            await empleado.PostAsJsonAsync("/api/accesos/celular", new { tokenQr = qrRespuesta.Token }));

        Assert.Equal("Autorizado", acceso.Estado);
        Assert.Equal(ComedorApiFactory.EmpleadoMatricula, acceso.Matricula);
        Assert.Equal("Ana Pérez", acceso.Nombre);
    }

    [Fact]
    public async Task Qr_ConRolAdministrador_DevuelveToken()
    {
        using var fabrica = await CrearFabricaAsync();
        var admin = ConToken(fabrica, await TokenSistemaAsync(fabrica, Roles.Administrador));

        var r = await admin.GetAsync($"/api/puntos/{ComedorApiFactory.PuntoId}/qr");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.False(string.IsNullOrEmpty((await r.Content.ReadFromJsonAsync<RespuestaQr>())!.Token));
    }

    [Fact]
    public async Task Qr_PuntoInexistente_404()
    {
        using var fabrica = await CrearFabricaAsync();
        var punto = ConToken(fabrica, await TokenSistemaAsync(fabrica, Roles.Chef));

        Assert.Equal(HttpStatusCode.NotFound, (await punto.GetAsync("/api/puntos/NOEXISTE/qr")).StatusCode);
    }

    [Fact]
    public async Task Solicitudes_AutorizadoAparecePorServir_ConNombre()
    {
        using var fabrica = await CrearFabricaAsync();
        var punto = ConToken(fabrica, await TokenSistemaAsync(fabrica, Roles.Chef));
        var empleado = ConToken(fabrica, await TokenEmpleadoAsync(fabrica));
        await empleado.PostAsJsonAsync("/api/accesos/celular", new { tokenQr = await TokenQrAsync(punto) });

        var lista = (await punto.GetFromJsonAsync<Solicitud[]>($"/api/puntos/{ComedorApiFactory.PuntoId}/solicitudes"))!;

        var fila = Assert.Single(lista);
        Assert.True(fila.Autorizado);
        Assert.False(fila.Servido);
        Assert.Equal("Ana Pérez", fila.Nombre);
        Assert.NotNull(fila.ConsumoId);
    }

    [Fact]
    public async Task Servido_MarcaServidoEnSolicitudes()
    {
        using var fabrica = await CrearFabricaAsync();
        var punto = ConToken(fabrica, await TokenSistemaAsync(fabrica, Roles.Chef));
        var empleado = ConToken(fabrica, await TokenEmpleadoAsync(fabrica));
        await empleado.PostAsJsonAsync("/api/accesos/celular", new { tokenQr = await TokenQrAsync(punto) });
        var id = await ConsumoIdAsync(punto);

        var r = await punto.PostAsync($"/api/consumos/{id}/servido", null);

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.False((await r.Content.ReadFromJsonAsync<Servido>())!.YaServido);
        var lista = (await punto.GetFromJsonAsync<Solicitud[]>($"/api/puntos/{ComedorApiFactory.PuntoId}/solicitudes"))!;
        Assert.True(Assert.Single(lista).Servido);
    }

    [Fact]
    public async Task Servido_ConsumoInexistente_404()
    {
        using var fabrica = await CrearFabricaAsync();
        var punto = ConToken(fabrica, await TokenSistemaAsync(fabrica, Roles.Chef));

        var r = await punto.PostAsync($"/api/consumos/{Guid.NewGuid()}/servido", null);

        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    [Fact]
    public async Task Servido_DosVecesSimultaneas_UnaEntrega()
    {
        using var fabrica = await CrearFabricaAsync();
        var punto = ConToken(fabrica, await TokenSistemaAsync(fabrica, Roles.Chef));
        var encargado = ConToken(fabrica, await TokenSistemaAsync(fabrica, Roles.Chef));
        var empleado = ConToken(fabrica, await TokenEmpleadoAsync(fabrica));
        await empleado.PostAsJsonAsync("/api/accesos/celular", new { tokenQr = await TokenQrAsync(punto) });
        var id = await ConsumoIdAsync(punto);

        var respuestas = await Task.WhenAll(
            punto.PostAsync($"/api/consumos/{id}/servido", null),
            encargado.PostAsync($"/api/consumos/{id}/servido", null));

        Assert.All(respuestas, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        var cuerpos = await Task.WhenAll(respuestas.Select(r => r.Content.ReadFromJsonAsync<Servido>()));
        Assert.Equal(1, cuerpos.Count(c => !c!.YaServido));
        Assert.Equal(1, cuerpos.Count(c => c!.YaServido));
        using var scope = fabrica.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ComedorDbContext>();
        Assert.Equal(1, await db.Entregas.CountAsync(e => e.ConsumoId == id));
    }

    [Fact]
    public async Task Concurrencia_CelularYManualSimultaneos_UnSoloConsumo()
    {
        using var fabrica = await CrearFabricaAsync();
        var punto = ConToken(fabrica, await TokenSistemaAsync(fabrica, Roles.Chef));
        var encargado = ConToken(fabrica, await TokenSistemaAsync(fabrica, Roles.Chef));
        var empleado = ConToken(fabrica, await TokenEmpleadoAsync(fabrica));
        var qr = await TokenQrAsync(punto);

        var respuestas = await Task.WhenAll(
            empleado.PostAsJsonAsync("/api/accesos/celular", new { tokenQr = qr }),
            encargado.PostAsJsonAsync("/api/accesos/manual",
                new { matricula = ComedorApiFactory.EmpleadoMatricula, puntoId = ComedorApiFactory.PuntoId }));

        Assert.All(respuestas, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        var accesos = await Task.WhenAll(respuestas.Select(r => r.Content.ReadFromJsonAsync<Acceso>()));
        Assert.Equal(1, accesos.Count(a => a!.Estado == "Autorizado"));
        var rechazado = Assert.Single(accesos, a => a!.Estado == "Rechazado");
        Assert.Equal(Motivo, rechazado!.Motivo);
        using var scope = fabrica.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ComedorDbContext>();
        Assert.Equal(1, await db.Consumos.CountAsync(c => c.Matricula == ComedorApiFactory.EmpleadoMatricula));
    }
}
