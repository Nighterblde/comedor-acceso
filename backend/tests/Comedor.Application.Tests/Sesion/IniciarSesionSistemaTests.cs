using Comedor.Application.Abstracciones;
using Comedor.Application.Sesion;
using Comedor.Application.Usuarios;
using Moq;

namespace Comedor.Application.Tests.Sesion;

public class IniciarSesionSistemaTests
{
    private static (IniciarSesionSistema caso, Mock<IHasherContrasena> hasher) Crear(UsuarioSistema? existente)
    {
        var repo = new Mock<IUsuarioSistemaRepositorio>();
        repo.Setup(r => r.ObtenerAsync(It.IsAny<string>())).ReturnsAsync(existente);
        var hasher = new Mock<IHasherContrasena>();
        hasher.Setup(h => h.Verificar(It.IsAny<UsuarioSistema>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);
        var caso = new IniciarSesionSistema(repo.Object, Mock.Of<IPuntoRepositorio>(), hasher.Object, Mock.Of<IEmisorJwt>(),
            new OpcionesJwt(), TimeProvider.System);
        return (caso, hasher);
    }

    [Fact]
    public async Task UsuarioInexistente_IgualVerificaContrasena()
    {
        var (caso, hasher) = Crear(null);

        var r = await caso.EjecutarAsync("nadie", "x");

        Assert.False(r.Exito);
        hasher.Verify(h => h.Verificar(It.IsAny<UsuarioSistema>(), It.IsAny<string>(), "x"), Times.Once);
    }

    [Fact]
    public async Task UsuarioInactivo_IgualVerificaContrasena()
    {
        var inactivo = new UsuarioSistema { Usuario = "u", HashContrasena = "h", Rol = Roles.Chef, Activo = false };
        var (caso, hasher) = Crear(inactivo);

        var r = await caso.EjecutarAsync("u", "x");

        Assert.False(r.Exito);
        hasher.Verify(h => h.Verificar(It.IsAny<UsuarioSistema>(), It.IsAny<string>(), "x"), Times.Once);
    }
}
