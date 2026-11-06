using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Comedor.Api.Tests.Persistencia;
using Comedor.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Comedor.Api.Tests;

[Collection("SqlServer")]
public class RolesTests(SqlServerFixture sql)
{
    private sealed record RespuestaToken(string Token);

    private static async Task<HttpClient> ClienteAsync(ComedorApiFactory fabrica, string rol)
    {
        var usuario = rol.ToLowerInvariant() + "-roles";
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
    public async Task Migracion_PuntoYEncargado_PasanAChef_SupervisorAAdministrador()
    {
        var nombre = "Comedor_Migracion_" + Guid.NewGuid().ToString("N");
        var cadena = sql.CadenaConexion.Replace("Database=master", "Database=" + nombre, StringComparison.OrdinalIgnoreCase);
        var opciones = new DbContextOptionsBuilder<ComedorDbContext>().UseSqlServer(cadena).Options;
        await using var db = new ComedorDbContext(opciones);
        var migrador = db.GetService<IMigrator>();
        await migrador.MigrateAsync("20261105003421_UsuariosSistema");
        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO UsuariosSistema (Usuario, HashContrasena, Rol, Activo) VALUES " +
            "('u-punto','h','Punto',1),('u-encargado','h','Encargado',1),('u-supervisor','h','Supervisor',1),('u-admin','h','Administrador',1)");

        await migrador.MigrateAsync();

        var roles = await db.Database.SqlQueryRaw<string>("SELECT Rol AS Value FROM UsuariosSistema ORDER BY Usuario").ToListAsync();
        // u-admin, u-encargado, u-punto, u-supervisor
        Assert.Equal(["Administrador", "Chef", "Chef", "Administrador"], roles);
        await db.Database.EnsureDeletedAsync();
    }

    [Fact]
    public async Task Reportes_ConRolChef_403()
    {
        using var fabrica = new ComedorApiFactory(sql);
        await fabrica.SembrarAsync();
        var c = await ClienteAsync(fabrica, "Chef");

        Assert.Equal(HttpStatusCode.Forbidden,
            (await c.GetAsync("/api/reportes/cotejo?desde=2026-11-10&hasta=2026-11-10")).StatusCode);
    }

    [Fact]
    public async Task Reportes_ConRolAdministrador_200()
    {
        using var fabrica = new ComedorApiFactory(sql);
        await fabrica.SembrarAsync();
        var c = await ClienteAsync(fabrica, "Administrador");

        Assert.Equal(HttpStatusCode.OK,
            (await c.GetAsync("/api/reportes/cotejo?desde=2026-11-10&hasta=2026-11-10")).StatusCode);
    }

    [Theory]
    [InlineData("Chef")]
    [InlineData("Administrador")]
    public async Task Servido_ConRolDelSistema_200(string rol)
    {
        using var fabrica = new ComedorApiFactory(sql);
        await fabrica.SembrarAsync();
        var admin = await ClienteAsync(fabrica, "Administrador");
        var c = await ClienteAsync(fabrica, rol);
        var qr = (await admin.GetFromJsonAsync<RespuestaQr>($"/api/puntos/{ComedorApiFactory.PuntoId}/qr"))!;
        var login = await fabrica.CreateClient().PostAsJsonAsync("/api/empleados/sesion",
            new { id = ComedorApiFactory.EmpleadoMatricula, contrasena = ComedorApiFactory.EmpleadoContrasena });
        var tokenEmpleado = (await login.Content.ReadFromJsonAsync<RespuestaToken>())!.Token;
        var empleado = fabrica.CreateClient();
        empleado.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenEmpleado);
        await empleado.PostAsJsonAsync("/api/accesos/celular", new { tokenQr = qr.Token });
        var lista = (await c.GetFromJsonAsync<Solicitud[]>($"/api/puntos/{ComedorApiFactory.PuntoId}/solicitudes"))!;

        var r = await c.PostAsync($"/api/consumos/{Assert.Single(lista).ConsumoId}/servido", null);

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }

    private sealed record RespuestaQr(string Token, string Url);

    private sealed record Solicitud(Guid? ConsumoId);
}
