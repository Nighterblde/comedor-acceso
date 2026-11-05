using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Comedor.Api.Tests.Persistencia;
using Comedor.Application.Usuarios;

namespace Comedor.Api.Tests;

[Collection("SqlServer")]
public class AuthTests(SqlServerFixture sql)
{
    private sealed record RespuestaLogin(string Token, string Rol, DateTime ExpiraEn);

    private sealed record RespuestaYo(string Usuario, string Rol);

    private static Task<HttpResponseMessage> Login(HttpClient c, string? usuario, string? contrasena) =>
        c.PostAsJsonAsync("/api/auth/login", new { usuario, contrasena });

    private static TimeSpan Vigencia(string token)
    {
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        return jwt.ValidTo - jwt.ValidFrom;
    }

    [Fact]
    public async Task Login_AdminSemilla_DevuelveToken12h()
    {
        using var fabrica = new ComedorApiFactory(sql);
        var cliente = fabrica.CreateClient();

        var respuesta = await Login(cliente, ComedorApiFactory.AdminUsuario, ComedorApiFactory.AdminContrasena);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var cuerpo = (await respuesta.Content.ReadFromJsonAsync<RespuestaLogin>())!;
        Assert.Equal(Roles.Administrador, cuerpo.Rol);
        Assert.Equal(TimeSpan.FromHours(12), Vigencia(cuerpo.Token));
        Assert.InRange(cuerpo.ExpiraEn - DateTime.UtcNow, TimeSpan.FromHours(11.9), TimeSpan.FromHours(12.1));
    }

    [Fact]
    public async Task Login_RolPunto_DevuelveToken30Dias()
    {
        using var fabrica = new ComedorApiFactory(sql);
        var cliente = fabrica.CreateClient();
        await fabrica.CrearUsuarioAsync("punto-entrada", "Punto-Prueba-123", Roles.Punto);

        var respuesta = await Login(cliente, "punto-entrada", "Punto-Prueba-123");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var cuerpo = (await respuesta.Content.ReadFromJsonAsync<RespuestaLogin>())!;
        Assert.Equal(Roles.Punto, cuerpo.Rol);
        Assert.Equal(TimeSpan.FromDays(30), Vigencia(cuerpo.Token));
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
