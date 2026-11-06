using System.Net;
using System.Net.Http.Json;
using Comedor.Api.Tests.Persistencia;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Comedor.Api.Tests;

[Collection("SqlServer")]
public class HospedajeTests(SqlServerFixture sql) : IDisposable
{
    private const string ContenidoIndex = "<!doctype html><title>Comedor de personal</title><app-root></app-root>";

    private readonly string _carpeta = Directory.CreateTempSubdirectory("comedor-hospedaje-").FullName;

    public void Dispose() => Directory.Delete(_carpeta, recursive: true);

    private sealed record RespuestaSalud(string Estado, string Version);

    private HttpClient ClienteConFrontend(ComedorApiFactory fabrica, bool conTunel = false)
    {
        var raiz = Path.Combine(_carpeta, "wwwroot");
        Directory.CreateDirectory(raiz);
        File.WriteAllText(Path.Combine(raiz, "index.html"), ContenidoIndex);
        var c = fabrica.WithWebHostBuilder(b => b.UseWebRoot(raiz)).CreateClient();
        if (conTunel)
        {
            c.DefaultRequestHeaders.Add("CF-Connecting-IP", "8.8.8.8");
        }

        return c;
    }

    [Fact]
    public async Task Salud_Anonimo_200ConVersion()
    {
        using var fabrica = new ComedorApiFactory(sql);

        var r = await fabrica.CreateClient().GetAsync("/api/salud");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var cuerpo = (await r.Content.ReadFromJsonAsync<RespuestaSalud>())!;
        Assert.Equal("ok", cuerpo.Estado);
        Assert.False(string.IsNullOrWhiteSpace(cuerpo.Version));
    }

    [Fact]
    public async Task Salud_DesdeTunel_200()
    {
        using var fabrica = new ComedorApiFactory(sql);
        var c = fabrica.CreateClient();
        c.DefaultRequestHeaders.Add("CF-Connecting-IP", "8.8.8.8");

        var r = await c.GetAsync("/api/salud");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/panel/cotejo")]
    [InlineData("/punto/P1")]
    public async Task RutaDelFrontend_DevuelveIndexHtml(string ruta)
    {
        using var fabrica = new ComedorApiFactory(sql);

        var r = await ClienteConFrontend(fabrica).GetAsync(ruta);

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal(ContenidoIndex, await r.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("/api/no-existe")]
    [InlineData("/api")]
    [InlineData("/API/otra/cosa")]
    public async Task RutaApiInexistente_404NoIndex(string ruta)
    {
        using var fabrica = new ComedorApiFactory(sql);

        var r = await ClienteConFrontend(fabrica).GetAsync(ruta);

        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
        Assert.DoesNotContain("app-root", await r.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task RutaApiProtegida_DesdeTunel_SigueEn403()
    {
        using var fabrica = new ComedorApiFactory(sql);

        var r = await ClienteConFrontend(fabrica, conTunel: true).GetAsync("/api/reportes/conteo");

        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }

    [Fact]
    public void ConfiguracionLocal_SobrescribeAppsettings()
    {
        var archivo = Path.Combine(_carpeta, "appsettings.Local.json");
        File.WriteAllText(archivo, """{ "Comedor": { "ZonaHoraria": "America/Mexico_City" } }""");
        using var fabrica = new ComedorApiFactory(sql);
        using var local = fabrica.WithWebHostBuilder(b => b.UseSetting("COMEDOR_CONFIG_LOCAL", archivo));

        var config = local.Services.GetRequiredService<IConfiguration>();

        Assert.Equal("America/Mexico_City", config["Comedor:ZonaHoraria"]);
    }
}
