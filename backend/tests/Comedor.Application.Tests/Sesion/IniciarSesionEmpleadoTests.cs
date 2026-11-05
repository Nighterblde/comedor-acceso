using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Comedor.Application.Abstracciones;
using Comedor.Application.Sesion;
using Comedor.Infrastructure.Externos;
using Comedor.Infrastructure.Seguridad;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace Comedor.Application.Tests.Sesion;

public class IniciarSesionEmpleadoTests
{
    private const string Clave = "clave-de-prueba-para-jwt-0123456789-abcdef";
    private static readonly DateTime Inicio = new(2026, 3, 2, 13, 30, 0);

    private sealed class RelojFalso : IReloj
    {
        public DateTime Valor { get; set; } = Inicio;

        public DateTime Ahora() => Valor;
    }

    private sealed class ProveedorFijo(DateTimeOffset ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora;
    }

    private readonly RelojFalso _reloj = new();
    private readonly Mock<IAutenticadorEmpresa> _autenticador = new();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

    private IniciarSesionEmpleado Crear(IEmisorJwt? emisor = null) =>
        new(_autenticador.Object,
            emisor ?? new EmisorJwt(Clave, new ProveedorFijo(new DateTimeOffset(2026, 3, 2, 20, 0, 0, TimeSpan.Zero))),
            new ControlIntentos(_cache, _reloj),
            new OpcionesSesion());

    private void Credenciales(string id, string contrasena, bool valido) =>
        _autenticador.Setup(a => a.ValidarAsync(id, contrasena)).ReturnsAsync(valido);

    [Fact]
    public async Task CredencialesCorrectas_DevuelveJwtRolEmpleado_SubMatricula_Vigencia30Dias()
    {
        Credenciales("AB123", "ok", true);

        var r = await Crear().EjecutarAsync("AB123", "ok");

        Assert.True(r.Exito);
        Assert.Null(r.Error);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(r.Token);
        Assert.Equal("AB123", jwt.Subject);
        Assert.Equal("Empleado", jwt.Claims.First(c => c.Type == ClaimTypes.Role).Value);
        Assert.Equal(TimeSpan.FromDays(30), jwt.ValidTo - jwt.ValidFrom);
        var iat = long.Parse(jwt.Claims.First(c => c.Type == "iat").Value);
        var exp = long.Parse(jwt.Claims.First(c => c.Type == "exp").Value);
        Assert.Equal(TimeSpan.FromDays(30).TotalSeconds, exp - iat);
    }

    [Fact]
    public async Task CredencialesIncorrectas_DevuelveError()
    {
        Credenciales("AB123", "mal", false);

        var r = await Crear().EjecutarAsync("AB123", "mal");

        Assert.False(r.Exito);
        Assert.Null(r.Token);
        Assert.Equal(ErrorSesion.CredencialesIncorrectas, r.Error);
    }

    [Fact]
    public async Task CincoFallos_SextoIntentoBloqueado_AunConContrasenaCorrecta()
    {
        Credenciales("AB123", "mal", false);
        Credenciales("AB123", "ok", true);
        var caso = Crear();

        for (var i = 0; i < 5; i++)
        {
            var f = await caso.EjecutarAsync("AB123", "mal");
            Assert.Equal(ErrorSesion.CredencialesIncorrectas, f.Error);
        }

        var r = await caso.EjecutarAsync("AB123", "ok");

        Assert.False(r.Exito);
        Assert.Equal(ErrorSesion.Bloqueado, r.Error);
        _autenticador.Verify(a => a.ValidarAsync("AB123", "ok"), Times.Never);
        _autenticador.Verify(a => a.ValidarAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(5));
    }

    [Fact]
    public async Task Bloqueo_SeLevantaA15Minutos()
    {
        Credenciales("AB123", "mal", false);
        Credenciales("AB123", "ok", true);
        var caso = Crear();
        for (var i = 0; i < 5; i++)
        {
            await caso.EjecutarAsync("AB123", "mal");
        }

        _reloj.Valor = Inicio.AddMinutes(14).AddSeconds(59);
        Assert.Equal(ErrorSesion.Bloqueado, (await caso.EjecutarAsync("AB123", "ok")).Error);

        _reloj.Valor = Inicio.AddMinutes(15);
        var r = await caso.EjecutarAsync("AB123", "ok");

        Assert.True(r.Exito);
    }

    [Fact]
    public async Task AutenticadorLanza_DevuelveServicioNoDisponible()
    {
        _autenticador.Setup(a => a.ValidarAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("sin respuesta"));
        var caso = Crear();

        for (var i = 0; i < 6; i++)
        {
            var r = await caso.EjecutarAsync("AB123", "x");
            Assert.False(r.Exito);
            Assert.Equal(ErrorSesion.ServicioNoDisponible, r.Error);
        }

        // No cuenta como fallo: nunca se llega al bloqueo.
        Assert.False(new ControlIntentos(_cache, _reloj).EstaBloqueado("AB123"));
    }

    [Fact]
    public async Task IdSeNormaliza()
    {
        Credenciales("AB123", "ok", true);

        var r = await Crear().EjecutarAsync(" ab123 ", "ok");

        Assert.True(r.Exito);
        _autenticador.Verify(a => a.ValidarAsync("AB123", "ok"), Times.Once);
        Assert.Equal("AB123", new JwtSecurityTokenHandler().ReadJwtToken(r.Token).Subject);
    }

    [Fact]
    public async Task AutenticadorSimulado_ComparaIdNormalizadoYContrasenaExacta()
    {
        var sim = new AutenticadorEmpresaSimulado(new Dictionary<string, string> { ["ab123"] = "Secreta" });

        Assert.True(await sim.ValidarAsync(" AB123 ", "Secreta"));
        Assert.False(await sim.ValidarAsync("AB123", "secreta"));
        Assert.False(await sim.ValidarAsync("ZZ999", "Secreta"));
    }

    [Fact]
    public void EmisorJwt_ClaveCorta_Lanza()
    {
        Assert.Throws<ArgumentException>(() => new EmisorJwt("corta"));
        Assert.Throws<ArgumentException>(() => new EmisorJwt(""));
    }
}
