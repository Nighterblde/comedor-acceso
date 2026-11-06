using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Comedor.Api.Tests.Persistencia;
using Comedor.Application.Abstracciones;
using Comedor.Application.Usuarios;
using Comedor.Infrastructure.Repositorios;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Comedor.Api.Tests;

[Collection("SqlServer")]
public class AuthTests(SqlServerFixture sql)
{
    private sealed record RespuestaLogin(string Token, string Rol, DateTime ExpiraEn, string? Inicio);

    private sealed record RespuestaToken(string Token);

    private sealed record RespuestaYo(string Usuario, string Rol);

    private static Task<HttpResponseMessage> Login(HttpClient c, string? usuario, string? contrasena) =>
        c.PostAsJsonAsync("/api/auth/login", new { usuario, contrasena });

    private static TimeSpan Vigencia(string token)
    {
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        return jwt.ValidTo - jwt.ValidFrom;
    }

    [Fact]
    public async Task Login_Chef_Expira3650Dias_EInicioEnPunto()
    {
        using var fabrica = new ComedorApiFactory(sql);
        await fabrica.SembrarAsync();
        var cliente = fabrica.CreateClient();
        await fabrica.CrearUsuarioAsync("chef-entrada", "Chef-Prueba-123", Roles.Chef);

        var respuesta = await Login(cliente, "chef-entrada", "Chef-Prueba-123");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var cuerpo = (await respuesta.Content.ReadFromJsonAsync<RespuestaLogin>())!;
        Assert.Equal(Roles.Chef, cuerpo.Rol);
        Assert.Equal(TimeSpan.FromDays(3650), Vigencia(cuerpo.Token));
        Assert.Equal("/punto/" + ComedorApiFactory.PuntoId, cuerpo.Inicio);
    }

    [Fact]
    public async Task Login_Administrador_Expira2Horas_EInicioEnPanel()
    {
        using var fabrica = new ComedorApiFactory(sql);
        var cliente = fabrica.CreateClient();

        var respuesta = await Login(cliente, ComedorApiFactory.AdminUsuario, ComedorApiFactory.AdminContrasena);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var cuerpo = (await respuesta.Content.ReadFromJsonAsync<RespuestaLogin>())!;
        Assert.Equal(Roles.Administrador, cuerpo.Rol);
        Assert.Equal(TimeSpan.FromHours(2), Vigencia(cuerpo.Token));
        Assert.InRange(cuerpo.ExpiraEn - DateTime.UtcNow, TimeSpan.FromHours(1.9), TimeSpan.FromHours(2.1));
        Assert.Equal("/panel", cuerpo.Inicio);
    }

    [Fact]
    public async Task Login_ChefSinPuntoActivo_409()
    {
        using var fabrica = new ComedorApiFactory(sql);
        var cliente = fabrica.CreateClient();
        await fabrica.CrearUsuarioAsync("chef-sin-punto", "Chef-Prueba-123", Roles.Chef);

        var respuesta = await Login(cliente, "chef-sin-punto", "Chef-Prueba-123");

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Contains("No hay un punto de entrada activo; pide al administrador que lo cree.",
            await respuesta.Content.ReadAsStringAsync());
    }

    private static async Task<HttpClient> ConSesionAsync(ComedorApiFactory fabrica, string usuario)
    {
        var login = (await (await Login(fabrica.CreateClient(), usuario, "Clave-Prueba-123"))
            .Content.ReadFromJsonAsync<RespuestaLogin>())!;
        var c = fabrica.CreateClient();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);
        return c;
    }

    [Fact]
    public async Task TokenChef_UsuarioDesactivado_401()
    {
        using var fabrica = new ComedorApiFactory(sql);
        await fabrica.SembrarAsync();
        await fabrica.CrearUsuarioAsync("chef-baja", "Clave-Prueba-123", Roles.Chef);
        var cliente = await ConSesionAsync(fabrica, "chef-baja");
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/api/auth/yo")).StatusCode);

        await fabrica.CrearUsuarioAsync("chef-baja", "Clave-Prueba-123", Roles.Chef, activo: false);

        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/api/auth/yo")).StatusCode);
    }

    [Fact]
    public async Task TokenAdministrador_RolCambiadoAChef_401EnReportes()
    {
        using var fabrica = new ComedorApiFactory(sql);
        await fabrica.CrearUsuarioAsync("admin-degradado", "Clave-Prueba-123", Roles.Administrador);
        var cliente = await ConSesionAsync(fabrica, "admin-degradado");
        const string ruta = "/api/reportes/cotejo?desde=2026-11-01&hasta=2026-11-15";
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync(ruta)).StatusCode);

        await fabrica.CrearUsuarioAsync("admin-degradado", "Clave-Prueba-123", Roles.Chef);

        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync(ruta)).StatusCode);
    }

    private sealed class Contador
    {
        public int Consultas;
    }

    private sealed class RepositorioContador(UsuarioSistemaRepositorio interno, Contador contador) : IUsuarioSistemaRepositorio
    {
        public Task<UsuarioSistema?> ObtenerAsync(string usuario)
        {
            contador.Consultas++;
            return interno.ObtenerAsync(usuario);
        }

        public Task<IReadOnlyList<UsuarioSistema>> TodosAsync() => interno.TodosAsync();

        public Task GuardarAsync(UsuarioSistema u) => interno.GuardarAsync(u);

        public Task<bool> ExisteAlgunoAsync() => interno.ExisteAlgunoAsync();
    }

    [Fact]
    public async Task TokenEmpleado_NoConsultaUsuariosDelSistema()
    {
        using var fabrica = new ComedorApiFactory(sql);
        await fabrica.SembrarAsync();
        var contador = new Contador();
        using var conContador = fabrica.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
        {
            s.AddSingleton(contador);
            s.AddScoped<IUsuarioSistemaRepositorio>(sp =>
                new RepositorioContador(ActivatorUtilities.CreateInstance<UsuarioSistemaRepositorio>(sp), contador));
        }));
        var sesion = await conContador.CreateClient().PostAsJsonAsync("/api/empleados/sesion",
            new { id = ComedorApiFactory.EmpleadoMatricula, contrasena = ComedorApiFactory.EmpleadoContrasena });
        Assert.Equal(HttpStatusCode.OK, sesion.StatusCode);
        var token = (await sesion.Content.ReadFromJsonAsync<RespuestaToken>())!.Token;
        var cliente = conContador.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var antes = contador.Consultas;

        var r = await cliente.PostAsJsonAsync("/api/accesos/celular", new { tokenQr = "x" });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal(antes, contador.Consultas);
    }

    [Fact]
    public async Task Login_ContrasenaIncorrecta_401()
    {
        using var fabrica = new ComedorApiFactory(sql);
        var cliente = fabrica.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await Login(cliente, ComedorApiFactory.AdminUsuario, "mala")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await Login(cliente, "no-existe", "x")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Login(cliente, null, null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Login(cliente, "", "")).StatusCode);
    }

    [Fact]
    public async Task Yo_SinToken_401()
    {
        using var fabrica = new ComedorApiFactory(sql);
        var cliente = fabrica.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/api/auth/yo")).StatusCode);
    }

    [Fact]
    public async Task Yo_ConToken_DevuelveUsuarioYRol()
    {
        using var fabrica = new ComedorApiFactory(sql);
        var cliente = fabrica.CreateClient();
        var login = (await (await Login(cliente, ComedorApiFactory.AdminUsuario, ComedorApiFactory.AdminContrasena))
            .Content.ReadFromJsonAsync<RespuestaLogin>())!;
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);

        var respuesta = await cliente.GetAsync("/api/auth/yo");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var yo = (await respuesta.Content.ReadFromJsonAsync<RespuestaYo>())!;
        Assert.Equal(ComedorApiFactory.AdminUsuario, yo.Usuario);
        Assert.Equal(Roles.Administrador, yo.Rol);
    }
}
