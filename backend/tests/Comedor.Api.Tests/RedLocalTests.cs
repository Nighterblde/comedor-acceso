using System.Net;
using System.Net.Http.Json;
using Comedor.Api.Configuracion;
using Comedor.Api.Tests.Persistencia;
using Microsoft.AspNetCore.Http;

namespace Comedor.Api.Tests;

[Collection("SqlServer")]
public class RedLocalTests(SqlServerFixture sql)
{
    private const string MensajeFuera = "Disponible solo desde la red de la empresa";

    private sealed record RespuestaError(string Error);

    private static IHeaderDictionary Encabezados(string? nombre = null, string? valor = null)
    {
        var e = new HeaderDictionary();
        if (nombre is not null)
        {
            e[nombre] = valor;
        }

        return e;
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("::1")]
    [InlineData("10.1.2.3")]
    [InlineData("172.20.0.1")]
    [InlineData("192.168.1.50")]
    [InlineData("::ffff:192.168.1.50")]
    public void EsRedLocal_IpsPrivadasYLoopback_True(string ip) =>
        Assert.True(RedLocal.EsRedLocal(IPAddress.Parse(ip), Encabezados()));

    [Fact]
    public void EsRedLocal_IpNula_True() => Assert.True(RedLocal.EsRedLocal(null, Encabezados()));

    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("2001:4860::1")]
    [InlineData("172.32.0.1")]
    [InlineData("::ffff:8.8.8.8")]
    public void EsRedLocal_IpPublica_False(string ip) =>
        Assert.False(RedLocal.EsRedLocal(IPAddress.Parse(ip), Encabezados()));

    [Theory]
    [InlineData("169.254.1.1")]
    [InlineData("fd00::1")]
    [InlineData("fe80::1")]
    public void EsRedLocal_RangosLocalesAdicionales_True(string ip) =>
        Assert.True(RedLocal.EsRedLocal(IPAddress.Parse(ip), Encabezados()));

    [Fact]
    public void EsRedLocal_ConCfConnectingIp_False() =>
        Assert.False(RedLocal.EsRedLocal(IPAddress.Loopback, Encabezados("CF-Connecting-IP", "8.8.8.8")));

    [Fact]
    public void EsRedLocal_ConXForwardedFor_False() =>
        Assert.False(RedLocal.EsRedLocal(IPAddress.Loopback, Encabezados("X-Forwarded-For", "8.8.8.8")));

    [Fact]
    public void RutasPublicas_SonLasCuatroDelCelular() =>
        Assert.Equal(
            ["/api/empleados/sesion", "/api/accesos/celular", "/api/accesos/mi-turno", "/api/salud"],
            RedLocal.RutasPublicas);

    private HttpClient ClienteTunel(ComedorApiFactory fabrica)
    {
        var c = fabrica.CreateClient();
        c.DefaultRequestHeaders.Add("CF-Connecting-IP", "8.8.8.8");
        return c;
    }

    [Fact]
    public async Task Login_DesdeTunel_403()
    {
        using var fabrica = new ComedorApiFactory(sql);
        await fabrica.SembrarAsync();

        var r = await ClienteTunel(fabrica).PostAsJsonAsync("/api/auth/login",
            new { usuario = ComedorApiFactory.AdminUsuario, contrasena = ComedorApiFactory.AdminContrasena });

        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
        Assert.Equal(MensajeFuera, (await r.Content.ReadFromJsonAsync<RespuestaError>())!.Error);
    }

    [Fact]
    public async Task Login_SinEncabezadosDeTunel_NoEs403()
    {
        using var fabrica = new ComedorApiFactory(sql);
        await fabrica.SembrarAsync();

        var r = await fabrica.CreateClient().PostAsJsonAsync("/api/auth/login",
            new { usuario = ComedorApiFactory.AdminUsuario, contrasena = ComedorApiFactory.AdminContrasena });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }

    [Theory]
    [InlineData("/api/reportes/conteo?desde=2026-11-10&hasta=2026-11-10")]
    [InlineData("/API/Reportes/conteo/")]
    public async Task Reportes_DesdeTunel_403(string ruta)
    {
        using var fabrica = new ComedorApiFactory(sql);
        await fabrica.SembrarAsync();

        var r = await ClienteTunel(fabrica).GetAsync(ruta);

        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
        Assert.Equal(MensajeFuera, (await r.Content.ReadFromJsonAsync<RespuestaError>())!.Error);
    }

    [Fact]
    public async Task SesionEmpleado_DesdeTunel_NoEs403()
    {
        using var fabrica = new ComedorApiFactory(sql);
        await fabrica.SembrarAsync();

        var r = await ClienteTunel(fabrica).PostAsJsonAsync("/api/empleados/sesion",
            new { id = ComedorApiFactory.EmpleadoMatricula, contrasena = ComedorApiFactory.EmpleadoContrasena });

        Assert.NotEqual(HttpStatusCode.Forbidden, r.StatusCode);
    }

    [Fact]
    public async Task AccesoCelular_DesdeTunel_NoEs403()
    {
        using var fabrica = new ComedorApiFactory(sql);
        await fabrica.SembrarAsync();

        var r = await ClienteTunel(fabrica).PostAsJsonAsync("/api/accesos/celular", new { tokenQr = "x" });

        Assert.NotEqual(HttpStatusCode.Forbidden, r.StatusCode);
    }

    [Fact]
    public async Task MiTurno_DesdeTunel_NoEs403()
    {
        using var fabrica = new ComedorApiFactory(sql);
        await fabrica.SembrarAsync();

        var r = await ClienteTunel(fabrica).GetAsync("/api/accesos/mi-turno");

        Assert.NotEqual(HttpStatusCode.Forbidden, r.StatusCode);
    }
}
