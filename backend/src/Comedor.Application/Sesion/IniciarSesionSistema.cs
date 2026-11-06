using Comedor.Application.Abstracciones;
using Comedor.Application.Usuarios;

namespace Comedor.Application.Sesion;

public record ResultadoSesionSistema(bool Exito, string? Token, string? Rol, DateTime? ExpiraEn, string? Inicio);

public class IniciarSesionSistema(
    IUsuarioSistemaRepositorio usuarios,
    IPuntoRepositorio puntos,
    IHasherContrasena hasher,
    IEmisorJwt emisor,
    OpcionesJwt opciones,
    TimeProvider tiempo)
{
    private static readonly ResultadoSesionSistema Fallo = new(false, null, null, null, null);

    // Hash PBKDF2 (PasswordHasher v3) de una contraseña ficticia: sirve para que un usuario inexistente
    // o inactivo cueste lo mismo que uno existente y no se pueda enumerar usuarios por tiempo de respuesta.
    private const string HashFicticio =
        "AQAAAAIAAYagAAAAEAABAgMEBQYHCAkKCwwNDg+V/nu6gFE94WAQKgVOedJNKt3xFvhRKlmoOVlVQTTXJQ==";

    private static readonly UsuarioSistema UsuarioFicticio = new() { Usuario = "-", Rol = "-", Activo = false };

    public async Task<ResultadoSesionSistema> EjecutarAsync(string? usuario, string? contrasena)
    {
        var nombre = usuario?.Trim();
        if (string.IsNullOrEmpty(nombre) || string.IsNullOrEmpty(contrasena))
        {
            return Fallo;
        }

        var u = await usuarios.ObtenerAsync(nombre);
        var valido = u is not null && u.Activo && string.Equals(u.Usuario, nombre, StringComparison.Ordinal);
        var contrasenaCorrecta = valido
            ? hasher.Verificar(u!, u!.HashContrasena, contrasena)
            : hasher.Verificar(UsuarioFicticio, HashFicticio, contrasena);
        if (!valido || !contrasenaCorrecta)
        {
            return Fallo;
        }

        string inicio;
        TimeSpan vigencia;
        if (u!.Rol == Roles.Chef)
        {
            var activo = (await puntos.TodosAsync()).Where(p => p.Activo).OrderBy(p => p.Id, StringComparer.Ordinal).FirstOrDefault();
            if (activo is null)
            {
                // Credenciales válidas pero sin punto al cual enviar al chef: el controlador responde 409.
                return new ResultadoSesionSistema(false, null, u.Rol, null, null);
            }

            inicio = $"/punto/{activo.Id}";
            vigencia = TimeSpan.FromDays(opciones.DiasChef);
        }
        else
        {
            inicio = "/panel";
            vigencia = TimeSpan.FromHours(opciones.HorasAdministrador);
        }

        var token = emisor.Emitir(u.Usuario, u.Rol, vigencia);
        return new ResultadoSesionSistema(true, token, u.Rol, tiempo.GetUtcNow().UtcDateTime + vigencia, inicio);
    }
}
