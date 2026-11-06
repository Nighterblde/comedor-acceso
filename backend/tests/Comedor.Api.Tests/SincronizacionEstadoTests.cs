using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Comedor.Api.Tests.Persistencia;
using Comedor.Application.Abstracciones;
using Comedor.Application.Sincronizacion;
using Comedor.Application.Usuarios;
using Microsoft.Extensions.DependencyInjection;

namespace Comedor.Api.Tests;

[Collection("SqlServer")]
public class SincronizacionEstadoTests(SqlServerFixture sql)
{
    private sealed record RespuestaToken(string Token);

    private sealed record Estado(bool UltimaFallida, DateTime? UltimaEjecucion, DateTime? UltimaExitosa);

    private static async Task<HttpClient> ClienteAsync(ComedorApiFactory fabrica, string rol)
    {
        var usuario = rol.ToLowerInvariant() + "-sinc";
        await fabrica.CrearUsuarioAsync(usuario, "Clave-Prueba-123", rol);
        var r = await fabrica.CreateClient().PostAsJsonAsync("/api/auth/login",
            new { usuario, contrasena = "Clave-Prueba-123" });
        var token = (await r.Content.ReadFromJsonAsync<RespuestaToken>())!.Token;
        var c = fabrica.CreateClient();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return c;
    }

    [Fact]
    public async Task Estado_Administrador_VeUltimaFallidaYFechaDeLaUltimaExitosa()
    {
        using var fabrica = new ComedorApiFactory(sql);
        await fabrica.SembrarAsync();
        var exitosa = new DateTime(2026, 11, 3, 2, 0, 0);
        var fallida = new DateTime(2026, 11, 4, 2, 0, 0);
        using (var scope = fabrica.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IBitacoraSincronizacionRepositorio>();
            await repo.RegistrarAsync(new BitacoraSincronizacion(Guid.NewGuid(), exitosa, true, 1, 0, 0, 0, null));
            await repo.RegistrarAsync(new BitacoraSincronizacion(Guid.NewGuid(), fallida, false, 0, 0, 0, 0, "caída"));
        }

        var c = await ClienteAsync(fabrica, Roles.Administrador);
        var r = await c.GetAsync("/api/sincronizacion/estado");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var estado = (await r.Content.ReadFromJsonAsync<Estado>())!;
        Assert.True(estado.UltimaFallida);
        Assert.Equal(fallida, estado.UltimaEjecucion);
        Assert.Equal(exitosa, estado.UltimaExitosa);
    }

    [Fact]
    public async Task Estado_Administrador200()
    {
        using var fabrica = new ComedorApiFactory(sql);
        await fabrica.SembrarAsync();
        var admin = await ClienteAsync(fabrica, Roles.Administrador);

        var r = await admin.GetAsync("/api/sincronizacion/estado");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.False((await r.Content.ReadFromJsonAsync<Estado>())!.UltimaFallida);
    }

    [Theory]
    [InlineData(Roles.Chef)]
    [InlineData(Roles.Empleado)]
    public async Task Estado_RolSinAcceso403(string rol)
    {
        using var fabrica = new ComedorApiFactory(sql);
        await fabrica.SembrarAsync();
        var token = fabrica.Services.GetRequiredService<IEmisorJwt>().Emitir("x-" + rol, rol, TimeSpan.FromHours(1));
        var c = fabrica.CreateClient();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync("/api/sincronizacion/estado")).StatusCode);
    }

    [Fact]
    public async Task Estado_SinToken401_YRolChef403()
    {
        using var fabrica = new ComedorApiFactory(sql);
        await fabrica.SembrarAsync();

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await fabrica.CreateClient().GetAsync("/api/sincronizacion/estado")).StatusCode);
        var chef = await ClienteAsync(fabrica, Roles.Chef);
        Assert.Equal(HttpStatusCode.Forbidden, (await chef.GetAsync("/api/sincronizacion/estado")).StatusCode);
    }
}
