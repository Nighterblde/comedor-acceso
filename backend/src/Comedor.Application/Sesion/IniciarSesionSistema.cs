using Comedor.Application.Abstracciones;
using Comedor.Application.Usuarios;

namespace Comedor.Application.Sesion;

public record ResultadoSesionSistema(bool Exito, string? Token, string? Rol, DateTime? ExpiraEn);

public class IniciarSesionSistema(
    IUsuarioSistemaRepositorio usuarios,
    IHasherContrasena hasher,
    IEmisorJwt emisor,
    OpcionesJwt opciones,
    TimeProvider tiempo)
{
    private static readonly ResultadoSesionSistema Fallo = new(false, null, null, null);

    public async Task<ResultadoSesionSistema> EjecutarAsync(string? usuario, string? contrasena)
    {
        var nombre = usuario?.Trim();
        if (string.IsNullOrEmpty(nombre) || string.IsNullOrEmpty(contrasena))
        {
            return Fallo;
        }

        var u = await usuarios.ObtenerAsync(nombre);
        if (u is null || !u.Activo || !string.Equals(u.Usuario, nombre, StringComparison.Ordinal)
            || !hasher.Verificar(u, u.HashContrasena, contrasena))
        {
            return Fallo;
        }

        var vigencia = u.Rol == Roles.Punto
            ? TimeSpan.FromDays(opciones.DiasPunto)
            : TimeSpan.FromHours(opciones.HorasSistema);
        var token = emisor.Emitir(u.Usuario, u.Rol, vigencia);
        return new ResultadoSesionSistema(true, token, u.Rol, tiempo.GetUtcNow().UtcDateTime + vigencia);
    }
}
